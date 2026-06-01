namespace MtgDeckLab.Engine.Parsing;

public sealed class ParseResult
{
    public IReadOnlyList<ParsedEntry> Entries { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool HasErrors => Errors.Count > 0;

    internal ParseResult(IEnumerable<ParsedEntry> entries, IEnumerable<string> errors)
    {
        Entries = entries.ToList().AsReadOnly();
        Errors = errors.ToList().AsReadOnly();
    }
}
