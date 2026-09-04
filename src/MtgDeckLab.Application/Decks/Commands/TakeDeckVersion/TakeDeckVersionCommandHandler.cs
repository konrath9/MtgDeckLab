using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Application.Decks.Commands.TakeDeckVersion;

public class TakeDeckVersionCommandHandler : IRequestHandler<TakeDeckVersionCommand, TakeDeckVersionResult>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly IDeckVersionRepository _versionRepo;
    private readonly DeckAnalyzer _analyzer;

    public TakeDeckVersionCommandHandler(
        IDeckRepository deckRepo, ICardRepository cardRepo, IDeckVersionRepository versionRepo, DeckAnalyzer analyzer)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _versionRepo = versionRepo;
        _analyzer = analyzer;
    }

    public async Task<TakeDeckVersionResult> Handle(TakeDeckVersionCommand request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId)
            throw new KeyNotFoundException($"Deck {request.DeckId} not found.");

        var cardIds = deck.Entries.Select(e => e.CardId).Distinct();
        var cards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);
        var analysis = _analyzer.Analyze(DeckAnalysisMapper.BuildForAnalysis(deck, cards));

        var versionNumber = await _versionRepo.GetNextVersionNumberAsync(request.DeckId, cancellationToken);
        var entrySnapshots = deck.Entries.Select(e => (e.CardId, e.Quantity, e.IsCommander, e.IsSideboard));

        var version = new DeckVersion(
            request.DeckId, versionNumber, analysis.Score.Score, analysis.Score.Grade, entrySnapshots);

        await _versionRepo.AddAsync(version, cancellationToken);
        await _versionRepo.SaveChangesAsync(cancellationToken);

        return new TakeDeckVersionResult(
            version.Id, version.VersionNumber, version.Score, version.Grade,
            version.TotalMainDeckCards, version.TotalSideboardCards, version.CreatedAt);
    }
}
