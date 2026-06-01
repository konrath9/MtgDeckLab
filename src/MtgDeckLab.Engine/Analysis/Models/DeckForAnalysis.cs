using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

public sealed class DeckForAnalysis
{
    private readonly List<DeckAnalysisEntry> _entries;

    public string DeckName { get; }
    public Format Format { get; }
    public IReadOnlyList<DeckAnalysisEntry> Entries => _entries.AsReadOnly();

    public IEnumerable<DeckAnalysisEntry> MainDeck =>
        _entries.Where(e => !e.IsSideboard && !e.IsCommander);
    public IEnumerable<DeckAnalysisEntry> CommanderSlot =>
        _entries.Where(e => e.IsCommander);
    public IEnumerable<DeckAnalysisEntry> Sideboard =>
        _entries.Where(e => e.IsSideboard);

    public DeckForAnalysis(string deckName, Format format, IEnumerable<DeckAnalysisEntry> entries)
    {
        DeckName = deckName;
        Format = format;
        _entries = entries.ToList();
    }
}
