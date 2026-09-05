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
        var errors = new List<AnalysisMessage>();
        var warnings = new List<AnalysisMessage>();

        var mainDeck = deck.MainDeck.ToList();
        var commanders = deck.CommanderSlot.ToList();
        var totalCards = mainDeck.Sum(e => e.Quantity) + commanders.Sum(e => e.Quantity);

        if (totalCards != 100)
            errors.Add(AnalysisMessage.Of(
                AnalysisMessageCodes.CommanderDeckSize, ("total", totalCards)));

        foreach (var entry in mainDeck)
        {
            if (!entry.IsBasicLand && entry.Quantity > 1)
                errors.Add(AnalysisMessage.Of(
                    AnalysisMessageCodes.CommanderSingleton,
                    ("card", entry.CardName), ("quantity", entry.Quantity)));
        }

        if (commanders.Count == 0)
            warnings.Add(AnalysisMessage.Of(AnalysisMessageCodes.CommanderMissing));
        else if (commanders.Count > 2)
            errors.Add(AnalysisMessage.Of(AnalysisMessageCodes.CommanderTooMany));

        foreach (var cmd in commanders)
        {
            if (!cmd.IsLegendary)
                errors.Add(AnalysisMessage.Of(
                    AnalysisMessageCodes.CommanderNotLegendary, ("card", cmd.CardName)));

            if (!cmd.IsCreature && !cmd.Types.Contains(CardType.Planeswalker))
                errors.Add(AnalysisMessage.Of(
                    AnalysisMessageCodes.CommanderInvalidType, ("card", cmd.CardName)));
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
                    // Letras WUBRG em vez do nome do enum: o símbolo da cor é o mesmo em qualquer
                    // idioma, então o argumento não precisa ser traduzido junto com a frase.
                    var symbols = string.Join(", ", violations.Select(ColorSymbol));
                    errors.Add(AnalysisMessage.Of(
                        AnalysisMessageCodes.CommanderColorIdentity,
                        ("card", entry.CardName), ("colors", symbols)));
                }
            }
        }

        return new AnalysisValidationResult(errors.Count == 0, errors, warnings);
    }

    private static AnalysisValidationResult ValidateConstructed(DeckForAnalysis deck)
    {
        var errors = new List<AnalysisMessage>();
        var warnings = new List<AnalysisMessage>();

        var mainDeck = deck.MainDeck.ToList();
        var totalMain = mainDeck.Sum(e => e.Quantity);
        var totalSideboard = deck.Sideboard.Sum(e => e.Quantity);

        if (totalMain < 60)
            errors.Add(AnalysisMessage.Of(
                AnalysisMessageCodes.ConstructedMinSize, ("total", totalMain)));

        if (totalSideboard > 15)
            errors.Add(AnalysisMessage.Of(
                AnalysisMessageCodes.ConstructedSideboardSize, ("total", totalSideboard)));

        foreach (var entry in mainDeck)
        {
            if (!entry.IsBasicLand && entry.Quantity > 4)
                errors.Add(AnalysisMessage.Of(
                    AnalysisMessageCodes.ConstructedMaxCopies,
                    ("card", entry.CardName), ("quantity", entry.Quantity)));
        }

        return new AnalysisValidationResult(errors.Count == 0, errors, warnings);
    }

    private static string ColorSymbol(Color color) => color switch
    {
        Color.White => "W",
        Color.Blue => "U",
        Color.Black => "B",
        Color.Red => "R",
        Color.Green => "G",
        _ => "C"
    };
}
