using MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks;

internal static class DeckAnalysisResponseMapper
{
    private const string CardArgument = "card";

    /// <summary>
    /// Traduz o resultado do Engine para o contrato da API.
    /// </summary>
    /// <param name="cards">
    /// Cartas do deck, usadas para trocar o nome canônico em inglês que o Engine coloca nas
    /// mensagens pelo nome impresso no idioma do usuário. O Engine continua raciocinando sobre o
    /// nome em inglês (é o que torna a análise determinística e o que casa "Plains" &amp; cia. no
    /// TypeDistributionAnalyzer); a troca acontece só na hora de mostrar.
    /// </param>
    public static DeckAnalysisResponse ToResponse(
        DeckAnalysisResult result,
        IAnalysisMessageLocalizer localizer,
        IEnumerable<Card> cards,
        string cardLanguage)
    {
        var displayNames = BuildDisplayNames(cards, cardLanguage);

        IReadOnlyList<LocalizedMessage> Localize(IEnumerable<AnalysisMessage> messages) =>
            localizer.LocalizeAll(messages.Select(m => WithDisplayName(m, displayNames)));

        return new DeckAnalysisResponse(
            result.ManaCurve,
            result.ColorDistribution,
            result.TypeDistribution,
            result.RoleDistribution,
            new LocalizedRoleCoverage(result.RoleCoverage.Entries, Localize(result.RoleCoverage.Warnings)),
            result.ManaBase,
            new LocalizedSynergyAnalysis(
                result.Synergy.Signals, result.Synergy.DominantTag, result.Synergy.DominantStrength,
                Localize(result.Synergy.LowSynergyWarnings)),
            new LocalizedValidationResult(
                result.Validation.IsValid,
                Localize(result.Validation.Errors),
                Localize(result.Validation.Warnings)),
            new LocalizedDeckScore(
                result.Score.Score, result.Score.Grade,
                Localize(result.Score.Warnings), result.Score.ComponentScores));
    }

    private static IReadOnlyDictionary<string, string> BuildDisplayNames(
        IEnumerable<Card> cards, string cardLanguage)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in cards)
        {
            var localized = card.NameIn(cardLanguage);
            if (!string.Equals(localized, card.Name, StringComparison.Ordinal))
                names[card.Name] = localized;
        }
        return names;
    }

    private static AnalysisMessage WithDisplayName(
        AnalysisMessage message, IReadOnlyDictionary<string, string> displayNames)
    {
        if (displayNames.Count == 0 ||
            !message.Args.TryGetValue(CardArgument, out var value) ||
            value is not string cardName ||
            !displayNames.TryGetValue(cardName, out var localizedName))
        {
            return message;
        }

        var args = message.Args.ToDictionary(a => a.Key, a => a.Value);
        args[CardArgument] = localizedName;
        return new AnalysisMessage(message.Code, args);
    }
}
