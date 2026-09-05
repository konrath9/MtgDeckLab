using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

public sealed class DeckForAnalysis
{
    private readonly List<DeckAnalysisEntry> _entries;

    public string DeckName { get; }
    public Format Format { get; }
    public IReadOnlyList<DeckAnalysisEntry> Entries => _entries.AsReadOnly();

    public IEnumerable<DeckAnalysisEntry> MainDeck =>
        _entries.Where(e => e.Section == DeckSection.Main);
    public IEnumerable<DeckAnalysisEntry> CommanderSlot =>
        _entries.Where(e => e.Section == DeckSection.Commander);
    public IEnumerable<DeckAnalysisEntry> Sideboard =>
        _entries.Where(e => e.Section == DeckSection.Sideboard);

    public DeckForAnalysis(string deckName, Format format, IEnumerable<DeckAnalysisEntry> entries)
    {
        DeckName = deckName;
        Format = format;
        _entries = entries.ToList();
    }
}
