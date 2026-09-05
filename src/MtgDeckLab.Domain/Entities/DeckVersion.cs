using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Entities;

public sealed class DeckVersion
{
    private readonly List<DeckVersionEntry> _entries;

    public Guid Id { get; private init; }
    public Guid DeckId { get; private init; }
    public int VersionNumber { get; private init; }
    public int Score { get; private init; }
    public string Grade { get; private init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private init; }
    public IReadOnlyList<DeckVersionEntry> Entries => _entries.AsReadOnly();

    public int TotalMainDeckCards =>
        _entries.Where(e => e.Section == DeckSection.Main).Sum(e => e.Quantity);
    public int TotalSideboardCards =>
        _entries.Where(e => e.Section == DeckSection.Sideboard).Sum(e => e.Quantity);

    private DeckVersion()
    {
        _entries = new();
    }

    public DeckVersion(
        Guid deckId,
        int versionNumber,
        int score,
        string grade,
        IEnumerable<(Guid CardId, int Quantity, DeckSection Section)> entries)
    {
        Id = Guid.NewGuid();
        DeckId = deckId;
        VersionNumber = versionNumber;
        Score = score;
        Grade = grade;
        CreatedAt = DateTimeOffset.UtcNow;
        _entries = entries
            .Select(e => new DeckVersionEntry(Id, e.CardId, e.Quantity, e.Section))
            .ToList();
    }
}
