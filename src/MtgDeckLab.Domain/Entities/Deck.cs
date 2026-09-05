using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Entities;

public sealed class Deck
{
    private readonly List<DeckEntry> _entries;

    public Guid Id { get; private init; }
    public Guid UserId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public Format Format { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlyList<DeckEntry> Entries => _entries.AsReadOnly();
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IEnumerable<DeckEntry> MainDeck => _entries.Where(e => e.Section == DeckSection.Main);
    public IEnumerable<DeckEntry> Sideboard => _entries.Where(e => e.Section == DeckSection.Sideboard);
    public IEnumerable<DeckEntry> CommanderSlot => _entries.Where(e => e.Section == DeckSection.Commander);
    public IEnumerable<DeckEntry> Maybeboard => _entries.Where(e => e.Section == DeckSection.Maybeboard);

    public int TotalMainDeckCards => MainDeck.Sum(e => e.Quantity);
    public int TotalSideboardCards => Sideboard.Sum(e => e.Quantity);
    public int TotalMaybeboardCards => Maybeboard.Sum(e => e.Quantity);

    private Deck()
    {
        _entries = new();
    }

    public Deck(string name, Format format, Guid userId, string? description = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Name = name;
        Format = format;
        Description = description;
        _entries = new();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string name)
    {
        Name = name;
        Touch();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        Touch();
    }

    public void AddEntry(Guid cardId, int quantity, DeckSection section = DeckSection.Main)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var existing = _entries.FirstOrDefault(e => e.CardId == cardId && e.Section == section);

        if (existing is not null)
            existing.AddQuantity(quantity);
        else
            _entries.Add(new DeckEntry(Id, cardId, quantity, section));

        Touch();
    }

    public void SetEntryQuantity(Guid cardId, int quantity, DeckSection section = DeckSection.Main)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        var entry = _entries.FirstOrDefault(e => e.CardId == cardId && e.Section == section);

        if (quantity == 0)
        {
            if (entry is not null) _entries.Remove(entry);
        }
        else if (entry is not null)
        {
            entry.SetQuantity(quantity);
        }
        else
        {
            _entries.Add(new DeckEntry(Id, cardId, quantity, section));
        }

        Touch();
    }

    public void RemoveEntry(Guid cardId, DeckSection section = DeckSection.Main)
    {
        var entry = _entries.FirstOrDefault(e => e.CardId == cardId && e.Section == section);

        if (entry is not null)
        {
            _entries.Remove(entry);
            Touch();
        }
    }

    public void ClearSideboard()
    {
        _entries.RemoveAll(e => e.Section == DeckSection.Sideboard);
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
