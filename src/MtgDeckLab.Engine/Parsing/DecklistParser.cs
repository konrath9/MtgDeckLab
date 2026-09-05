using System.Text.RegularExpressions;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Parsing;

public sealed partial class DecklistParser
{
    // Matches lines starting with // (section headers like "// Sideboard", "// Creatures")
    [GeneratedRegex(@"^//\s*(.+)$")]
    private static partial Regex SectionHeaderRegex();

    // Commander tag as trailing #Commander or #CMDR
    [GeneratedRegex(@"\s+#(Commander|CMDR)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex CommanderHashTagRegex();

    // Commander tag as trailing *Commander* or *CMDR*
    [GeneratedRegex(@"\s+\*(Commander|CMDR)\*\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex CommanderStarTagRegex();

    // Set code: (XXX) optionally followed by a collector number
    [GeneratedRegex(@"\s+\(([A-Z0-9]{2,6})\)(?:\s+(\d+))?\s*$")]
    private static partial Regex SetCodeRegex();

    /// <summary>
    /// Parses one block of decklist text. <paramref name="defaultSection"/> is the section
    /// assigned to lines that don't carry an explicit inline tag (SB:/#Commander/// Sideboard) —
    /// lets a caller feed dedicated Main/Commander/Sideboard/Maybeboard textareas through the
    /// same parser while still honoring inline tags for a fully-pasted multi-section decklist.
    /// </summary>
    public ParseResult Parse(string input, DeckSection defaultSection = DeckSection.Main)
    {
        var entries = new List<ParsedEntry>();
        var errors = new List<string>();
        var inSideboard = false;

        foreach (var rawLine in input.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            var sectionMatch = SectionHeaderRegex().Match(line);
            if (sectionMatch.Success)
            {
                if (sectionMatch.Groups[1].Value.Trim().Equals("Sideboard", StringComparison.OrdinalIgnoreCase))
                    inSideboard = true;
                continue;
            }

            if (TryParseEntry(line, inSideboard, defaultSection, out var entry))
                entries.Add(entry!);
            else
                errors.Add(line);
        }

        return new ParseResult(entries, errors);
    }

    private static bool TryParseEntry(
        string line, bool currentSectionIsSideboard, DeckSection defaultSection, out ParsedEntry? entry)
    {
        entry = null;

        var section = currentSectionIsSideboard ? DeckSection.Sideboard : defaultSection;
        var text = line;

        if (text.StartsWith("SB:", StringComparison.OrdinalIgnoreCase))
        {
            section = DeckSection.Sideboard;
            text = text[3..].TrimStart();
        }

        // Extract quantity (one or more digits)
        var i = 0;
        while (i < text.Length && char.IsDigit(text[i])) i++;
        if (i == 0 || !int.TryParse(text[..i], out var quantity))
            return false;

        // Skip optional 'x' suffix on quantity
        if (i < text.Length && (text[i] == 'x' || text[i] == 'X')) i++;

        // Require at least one whitespace between quantity and name
        if (i >= text.Length || !char.IsWhiteSpace(text[i]))
            return false;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;

        var remainder = text[i..];
        if (string.IsNullOrEmpty(remainder))
            return false;

        // Strip commander tag from end — takes precedence over any other section signal, since
        // it's the most specific explicit declaration a line can carry.
        var hashMatch = CommanderHashTagRegex().Match(remainder);
        if (hashMatch.Success)
        {
            section = DeckSection.Commander;
            remainder = remainder[..hashMatch.Index];
        }
        else
        {
            var starMatch = CommanderStarTagRegex().Match(remainder);
            if (starMatch.Success)
            {
                section = DeckSection.Commander;
                remainder = remainder[..starMatch.Index];
            }
        }

        // Strip set code from end
        string? setCode = null;
        int? collectorNumber = null;
        var setMatch = SetCodeRegex().Match(remainder);
        if (setMatch.Success)
        {
            setCode = setMatch.Groups[1].Value;
            if (setMatch.Groups[2].Success && int.TryParse(setMatch.Groups[2].Value, out var cn))
                collectorNumber = cn;
            remainder = remainder[..setMatch.Index];
        }

        var cardName = remainder.Trim();
        if (string.IsNullOrEmpty(cardName))
            return false;

        entry = new ParsedEntry(quantity, cardName, section, setCode, collectorNumber);
        return true;
    }
}
