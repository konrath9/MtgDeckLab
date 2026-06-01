namespace MtgDeckLab.Domain.Entities;

public sealed class DeckEntry
{
    public Guid Id { get; private init; }
    public Guid DeckId { get; private init; }
    public Guid CardId { get; private init; }
    public int Quantity { get; private set; }
    public bool IsCommander { get; private init; }
    public bool IsSideboard { get; private init; }

    private DeckEntry() { }

    internal DeckEntry(Guid deckId, Guid cardId, int quantity, bool isCommander, bool isSideboard)
    {
        Id = Guid.NewGuid();
        DeckId = deckId;
        CardId = cardId;
        Quantity = quantity;
        IsCommander = isCommander;
        IsSideboard = isSideboard;
    }

    internal void AddQuantity(int amount) => Quantity += amount;
    internal void SetQuantity(int quantity) => Quantity = quantity;
}
