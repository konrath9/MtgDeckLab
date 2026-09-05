using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Entities;

public sealed class DeckVersionEntry
{
    public Guid Id { get; private init; }
    public Guid DeckVersionId { get; private init; }
    public Guid CardId { get; private init; }
    public int Quantity { get; private init; }
    public DeckSection Section { get; private init; }

    private DeckVersionEntry() { }

    internal DeckVersionEntry(Guid deckVersionId, Guid cardId, int quantity, DeckSection section)
    {
        Id = Guid.NewGuid();
        DeckVersionId = deckVersionId;
        CardId = cardId;
        Quantity = quantity;
        Section = section;
    }
}
