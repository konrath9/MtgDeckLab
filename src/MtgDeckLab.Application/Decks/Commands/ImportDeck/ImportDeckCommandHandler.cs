using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
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
        var parsedEntries = new List<ParsedEntry>();
        parsedEntries.AddRange(_parser.Parse(request.MainDecklist, DeckSection.Main).Entries);
        if (!string.IsNullOrWhiteSpace(request.CommanderDecklist))
            parsedEntries.AddRange(_parser.Parse(request.CommanderDecklist, DeckSection.Commander).Entries);
        if (!string.IsNullOrWhiteSpace(request.SideboardDecklist))
            parsedEntries.AddRange(_parser.Parse(request.SideboardDecklist, DeckSection.Sideboard).Entries);
        if (!string.IsNullOrWhiteSpace(request.MaybeboardDecklist))
            parsedEntries.AddRange(_parser.Parse(request.MaybeboardDecklist, DeckSection.Maybeboard).Entries);

        var distinctNames = parsedEntries.Select(e => e.CardName).Distinct();
        var cards = await _cardRepo.FindByNamesAsync(distinctNames, cancellationToken);

        // Uma decklist pode vir em qualquer idioma sincronizado (ou misturada), então cada carta
        // entra no índice pelo nome em inglês E por cada nome traduzido que tenha.
        var cardsByName = new Dictionary<string, Card>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in cards)
            foreach (var name in card.LocalizedNames.Select(n => n.Name).Append(card.Name))
            {
                cardsByName[name] = card;

                // Modal double-faced/split cards are stored under their full "Front // Back" name,
                // but a decklist entry only ever gives the front face.
                var frontFace = name.Split(" // ")[0];
                if (frontFace != name) cardsByName[frontFace] = card;
            }

        var deck = new Deck(request.DeckName, request.Format, request.UserId, request.Description);
        var unresolved = new List<UnresolvedCardName>();

        foreach (var entry in parsedEntries)
        {
            if (cardsByName.TryGetValue(entry.CardName, out var card))
                deck.AddEntry(card.Id, entry.Quantity, entry.Section);
            else
                unresolved.Add(new UnresolvedCardName(entry.CardName, entry.Section));
        }

        await _deckRepo.AddAsync(deck, cancellationToken);
        await _deckRepo.SaveChangesAsync(cancellationToken);

        return new ImportDeckResult(
            deck.Id,
            parsedEntries.Count - unresolved.Count,
            unresolved.AsReadOnly());
    }
}
