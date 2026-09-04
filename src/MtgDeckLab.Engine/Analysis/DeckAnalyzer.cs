using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public sealed class DeckAnalyzer
{
    public DeckAnalysisResult Analyze(DeckForAnalysis deck)
    {
        var mainDeck = deck.MainDeck.ToList();

        var manaCurve = ManaCurveAnalyzer.Analyze(mainDeck);
        var colorDistribution = ColorDistributionAnalyzer.Analyze(mainDeck);
        var typeDistribution = TypeDistributionAnalyzer.Analyze(mainDeck);
        var roleDistribution = CardRoleAnalyzer.Analyze(mainDeck);
        var roleCoverage = RoleCoverageAnalyzer.Analyze(roleDistribution, deck.Format);
        var validation = FormatValidator.Validate(deck);
        var score = DeckScorer.Score(deck, manaCurve, colorDistribution, typeDistribution, validation);

        return new DeckAnalysisResult(
            manaCurve, colorDistribution, typeDistribution, roleDistribution, roleCoverage, validation, score);
    }
}
