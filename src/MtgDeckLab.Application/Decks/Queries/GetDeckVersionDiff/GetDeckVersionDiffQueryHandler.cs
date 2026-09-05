using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckVersionDiff;

public class GetDeckVersionDiffQueryHandler : IRequestHandler<GetDeckVersionDiffQuery, DeckVersionDiff?>
{
    private readonly IDeckRepository _deckRepo;
    private readonly IDeckVersionRepository _versionRepo;
    private readonly ICardRepository _cardRepo;
    private readonly DeckAnalyzer _analyzer;

    public GetDeckVersionDiffQueryHandler(
        IDeckRepository deckRepo, IDeckVersionRepository versionRepo, ICardRepository cardRepo, DeckAnalyzer analyzer)
    {
        _deckRepo = deckRepo;
        _versionRepo = versionRepo;
        _cardRepo = cardRepo;
        _analyzer = analyzer;
    }

    public async Task<DeckVersionDiff?> Handle(GetDeckVersionDiffQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        var fromVersion = await _versionRepo.GetByIdAsync(request.DeckId, request.FromVersionId, cancellationToken);
        var toVersion = await _versionRepo.GetByIdAsync(request.DeckId, request.ToVersionId, cancellationToken);
        if (fromVersion is null || toVersion is null) return null;

        var cardIds = fromVersion.Entries.Select(e => e.CardId)
            .Concat(toVersion.Entries.Select(e => e.CardId))
            .Distinct();
        var cards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);
        var cardById = cards.ToDictionary(c => c.Id);

        var fromAnalysis = _analyzer.Analyze(DeckAnalysisMapper.BuildForAnalysis(
            deck.Name, deck.Format, ToEntryTuples(fromVersion), cards));
        var toAnalysis = _analyzer.Analyze(DeckAnalysisMapper.BuildForAnalysis(
            deck.Name, deck.Format, ToEntryTuples(toVersion), cards));

        // Custo calculado com os preços ATUAIS das cartas (não há preço histórico por versão —
        // isso é o que FinanceSnapshot rastreia à parte). Reflete quanto a mudança de composição
        // custaria hoje, não o custo real em cada momento.
        var costBefore = MainDeckCostUsd(fromVersion, cardById);
        var costAfter = MainDeckCostUsd(toVersion, cardById);

        var (added, removed, changed) = DiffEntries(fromVersion, toVersion, cardById);

        return new DeckVersionDiff(
            fromVersion.Id, fromVersion.VersionNumber,
            toVersion.Id, toVersion.VersionNumber,
            fromVersion.Score, toVersion.Score, toVersion.Score - fromVersion.Score,
            fromVersion.Grade, toVersion.Grade,
            fromVersion.TotalMainDeckCards, toVersion.TotalMainDeckCards,
            fromAnalysis.ManaCurve.AverageCmc, toAnalysis.ManaCurve.AverageCmc,
            costBefore, costAfter,
            added, removed, changed);
    }

    private static IEnumerable<(Guid CardId, int Quantity, DeckSection Section)> ToEntryTuples(
        DeckVersion version) =>
        version.Entries.Select(e => (e.CardId, e.Quantity, e.Section));

    private static decimal MainDeckCostUsd(DeckVersion version, IReadOnlyDictionary<Guid, Card> cardById) =>
        version.Entries
            .Where(e => e.Section == DeckSection.Main)
            .Sum(e => cardById.TryGetValue(e.CardId, out var card) ? (card.PriceUsd ?? 0m) * e.Quantity : 0m);

    private static (
        IReadOnlyList<DeckVersionCardChange> Added,
        IReadOnlyList<DeckVersionCardChange> Removed,
        IReadOnlyList<DeckVersionCardChange> Changed) DiffEntries(
        DeckVersion from, DeckVersion to, IReadOnlyDictionary<Guid, Card> cardById)
    {
        var fromByKey = from.Entries.ToDictionary(e => (e.CardId, e.Section));
        var toByKey = to.Entries.ToDictionary(e => (e.CardId, e.Section));

        var added = new List<DeckVersionCardChange>();
        var removed = new List<DeckVersionCardChange>();
        var changed = new List<DeckVersionCardChange>();

        foreach (var key in fromByKey.Keys.Union(toByKey.Keys))
        {
            fromByKey.TryGetValue(key, out var fromEntry);
            toByKey.TryGetValue(key, out var toEntry);
            var quantityBefore = fromEntry?.Quantity ?? 0;
            var quantityAfter = toEntry?.Quantity ?? 0;
            if (quantityBefore == quantityAfter) continue;

            var (cardId, section) = key;
            var cardName = cardById.TryGetValue(cardId, out var card) ? card.Name : "Unknown card";
            var change = new DeckVersionCardChange(
                cardId, cardName, quantityBefore, quantityAfter, section);

            if (quantityBefore == 0) added.Add(change);
            else if (quantityAfter == 0) removed.Add(change);
            else changed.Add(change);
        }

        return (added, removed, changed);
    }
}
