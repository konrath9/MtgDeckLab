namespace MtgDeckLab.Domain.Entities;

public sealed class DeckVersionEntry
{
    public Guid Id { get; private init; }
    public Guid DeckVersionId { get; private init; }
    public Guid CardId { get; private init; }
    public int Quantity { get; private init; }
    public bool IsCommander { get; private init; }
    public bool IsSideboard { get; private init; }

    private DeckVersionEntry() { }

    internal DeckVersionEntry(Guid deckVersionId, Guid cardId, int quantity, bool isCommander, bool isSideboard)
    {
        Id = Guid.NewGuid();
        DeckVersionId = deckVersionId;
        CardId = cardId;
        Quantity = quantity;
        IsCommander = isCommander;
        IsSideboard = isSideboard;
    }
}
