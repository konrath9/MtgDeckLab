using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public static class DeckScorer
{
    public static DeckScore Score(
        DeckForAnalysis deck,
        ManaCurve manaCurve,
        ColorDistribution colorDistribution,
        TypeDistribution typeDistribution,
        AnalysisValidationResult validation)
    {
        var manaCurveScore = ScoreManaCurve(manaCurve, deck.Format);
        var landRatioScore = ScoreLandRatio(typeDistribution, deck.Format);
        var colorScore = ScoreColorConsistency(colorDistribution);
        var complianceScore = ScoreRuleCompliance(validation);

        var weightedScore =
            manaCurveScore * 0.30 +
            landRatioScore * 0.30 +
            complianceScore * 0.25 +
            colorScore * 0.15;

        var finalScore = (int)Math.Round(weightedScore);

        var warnings = BuildWarnings(deck.Format, manaCurve, typeDistribution, colorDistribution);

        var components = new Dictionary<string, int>
        {
            ["ManaCurve"] = manaCurveScore,
            ["LandRatio"] = landRatioScore,
            ["ColorConsistency"] = colorScore,
            ["RuleCompliance"] = complianceScore
        };

        return new DeckScore(finalScore, ToGrade(finalScore), warnings, components);
    }

    private static int ScoreManaCurve(ManaCurve curve, Format format)
    {
        if (curve.TotalNonLandCards == 0) return 50;

        var avg = (double)curve.AverageCmc;

        return format == Format.Commander
            ? avg switch
            {
                >= 2.5 and <= 3.8 => 100,
                >= 2.0 and <= 4.3 => 80,
                >= 1.5 and <= 5.0 => 60,
                _ => 40
            }
            : avg switch
            {
                >= 1.8 and <= 2.8 => 100,
                >= 1.5 and <= 3.2 => 80,
                >= 1.0 and <= 3.8 => 60,
                _ => 40
            };
    }

    private static int ScoreLandRatio(TypeDistribution types, Format format)
    {
        if (types.Total == 0) return 0;

        var ratio = (double)types.Lands / types.Total * 100;

        return format == Format.Commander
            ? ratio switch
            {
                >= 35 and <= 41 => 100,
                >= 30 and <= 46 => 75,
                >= 25 and <= 51 => 50,
                _ => 25
            }
            : ratio switch
            {
                >= 33 and <= 43 => 100,
                >= 28 and <= 48 => 75,
                >= 23 and <= 53 => 50,
                _ => 25
            };
    }

    private static int ScoreColorConsistency(ColorDistribution colors) =>
        colors.CardCount.Count switch
        {
            0 => 90,
            1 => 100,
            2 => 90,
            3 => 70,
            4 => 50,
            _ => 35
        };

    private static int ScoreRuleCompliance(AnalysisValidationResult validation) =>
        validation.IsValid ? 100 : Math.Max(0, 100 - validation.Errors.Count * 25);

    private static string ToGrade(int score) => score switch
    {
        >= 80 => "A",
        >= 65 => "B",
        >= 50 => "C",
        >= 35 => "D",
        _ => "F"
    };

    private static IReadOnlyList<AnalysisMessage> BuildWarnings(
        Format format,
        ManaCurve manaCurve,
        TypeDistribution types,
        ColorDistribution colors)
    {
        var warnings = new List<AnalysisMessage>();

        // Números vão crus nos argumentos (não formatados): quem renderiza a frase é que sabe a
        // cultura do usuário — "3.42" em en-US e "3,42" em pt-BR.
        var avgCmc = (double)manaCurve.AverageCmc;
        if (format == Format.Commander && avgCmc > 4.0)
            warnings.Add(AnalysisMessage.Of(
                AnalysisMessageCodes.ScoreHighAverageCmcCommander, ("averageCmc", manaCurve.AverageCmc)));
        else if (format != Format.Commander && avgCmc > 3.5)
            warnings.Add(AnalysisMessage.Of(
                AnalysisMessageCodes.ScoreHighAverageCmcConstructed, ("averageCmc", manaCurve.AverageCmc)));

        if (format == Format.Commander && types.Lands < 30)
            warnings.Add(AnalysisMessage.Of(
                AnalysisMessageCodes.ScoreFewLandsCommander, ("lands", types.Lands)));
        else if (format != Format.Commander && types.Lands < 18)
            warnings.Add(AnalysisMessage.Of(
                AnalysisMessageCodes.ScoreFewLandsConstructed, ("lands", types.Lands)));

        if (types.Creatures == 0 && types.Planeswalkers == 0)
            warnings.Add(AnalysisMessage.Of(AnalysisMessageCodes.ScoreNoWinCondition));

        if (colors.CardCount.Count >= 4)
            warnings.Add(AnalysisMessage.Of(
                AnalysisMessageCodes.ScoreManyColors, ("colors", colors.CardCount.Count)));

        return warnings.AsReadOnly();
    }
}
