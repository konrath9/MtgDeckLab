using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Engine.Parsing;

namespace MtgDeckLab.Application.Decks.Commands.ImportDeck;

public class ImportDeckCommandHandler : IRequestHandler<ImportDeckCommand, ImportDeckResult>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly DecklistParser _parser;

    public ImportDeckCommandHandler(
        IDeckRepository deckRepo,
        ICardRepository cardRepo,
        DecklistParser parser)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _parser = parser;
    }

    public async Task<ImportDeckResult> Handle(ImportDeckCommand request, CancellationToken cancellationToken)
    {
        var parseResult = _parser.Parse(request.RawDecklist);

        var distinctNames = parseResult.Entries.Select(e => e.CardName).Distinct();
        var cards = await _cardRepo.FindByNamesAsync(distinctNames, cancellationToken);
        var cardsByName = cards.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        var deck = new Deck(request.DeckName, request.Format, request.UserId, request.Description);
        var unresolved = new List<string>();

        foreach (var entry in parseResult.Entries)
        {
            if (cardsByName.TryGetValue(entry.CardName, out var card))
                deck.AddEntry(card.Id, entry.Quantity, entry.IsSideboard, entry.IsCommander);
            else
                unresolved.Add(entry.CardName);
        }

        await _deckRepo.AddAsync(deck, cancellationToken);
        await _deckRepo.SaveChangesAsync(cancellationToken);

        return new ImportDeckResult(
            deck.Id,
            parseResult.Entries.Count - unresolved.Count,
            unresolved.AsReadOnly());
    }
}
