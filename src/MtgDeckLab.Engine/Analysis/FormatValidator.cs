using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public static class FormatValidator
{
    public static AnalysisValidationResult Validate(DeckForAnalysis deck) =>
        deck.Format == Format.Commander
            ? ValidateCommander(deck)
            : ValidateConstructed(deck);

    private static AnalysisValidationResult ValidateCommander(DeckForAnalysis deck)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var mainDeck = deck.MainDeck.ToList();
        var commanders = deck.CommanderSlot.ToList();
        var totalCards = mainDeck.Sum(e => e.Quantity) + commanders.Sum(e => e.Quantity);

        if (totalCards != 100)
            errors.Add($"Commander decks must have exactly 100 cards ({totalCards} found).");

        foreach (var entry in mainDeck)
        {
            if (!entry.IsBasicLand && entry.Quantity > 1)
                errors.Add($"'{entry.CardName}' has {entry.Quantity} copies — Commander is singleton (max 1 for non-basics).");
        }

        if (commanders.Count == 0)
            warnings.Add("No commander designated. Tag the commander with #Commander.");
        else if (commanders.Count > 2)
            errors.Add("A deck can have at most 2 commanders (partner rule).");

        foreach (var cmd in commanders)
        {
            if (!cmd.IsLegendary)
                errors.Add($"'{cmd.CardName}' is not Legendary and cannot be a commander.");

            if (!cmd.IsCreature && !cmd.Types.Contains(CardType.Planeswalker))
                errors.Add($"'{cmd.CardName}' must be a creature or planeswalker to be a commander.");
        }

        // Color identity compliance — only when a commander is designated
        if (commanders.Count > 0)
        {
            var commanderIdentity = commanders
                .SelectMany(c => c.ColorIdentity)
                .ToHashSet();

            foreach (var entry in mainDeck)
            {
                var violations = entry.ColorIdentity
                    .Where(c => !commanderIdentity.Contains(c))
                    .ToList();

                if (violations.Count > 0)
                {
                    var names = string.Join(", ", violations);
                    errors.Add($"'{entry.CardName}' has color identity outside the commander's ({names}).");
                }
            }
        }

        return new AnalysisValidationResult(errors.Count == 0, errors, warnings);
    }

    private static AnalysisValidationResult ValidateConstructed(DeckForAnalysis deck)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var mainDeck = deck.MainDeck.ToList();
        var totalMain = mainDeck.Sum(e => e.Quantity);
        var totalSideboard = deck.Sideboard.Sum(e => e.Quantity);

        if (totalMain < 60)
            errors.Add($"Deck must have at least 60 cards ({totalMain} found).");

        if (totalSideboard > 15)
            errors.Add($"Sideboard must have at most 15 cards ({totalSideboard} found).");

        foreach (var entry in mainDeck)
        {
            if (!entry.IsBasicLand && entry.Quantity > 4)
                errors.Add($"'{entry.CardName}' has {entry.Quantity} copies (max 4 for non-basics).");
        }

        return new AnalysisValidationResult(errors.Count == 0, errors, warnings);
    }
}
