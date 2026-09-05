using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Entities;

public sealed class DeckEntry
{
    public Guid Id { get; private init; }
    public Guid DeckId { get; private init; }
    public Guid CardId { get; private init; }
    public int Quantity { get; private set; }
    public DeckSection Section { get; private init; }

    private DeckEntry() { }

    internal DeckEntry(Guid deckId, Guid cardId, int quantity, DeckSection section)
    {
        Id = Guid.NewGuid();
        DeckId = deckId;
        CardId = cardId;
        Quantity = quantity;
        Section = section;
    }

    internal void AddQuantity(int amount) => Quantity += amount;
    internal void SetQuantity(int quantity) => Quantity = quantity;
}
