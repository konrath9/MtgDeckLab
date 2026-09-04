using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis;

// Mesmo estilo de heurística determinística do CardRoleClassifier (sem IA), mas orientada a
// "temas"/mecânicas de arquétipo em vez de papel funcional. Uma carta pode ter várias tags.
public static class SynergyTagClassifier
{
    public static IReadOnlyList<SynergyTag> Classify(string? oracleText)
    {
        if (string.IsNullOrWhiteSpace(oracleText)) return [];

        var text = oracleText.ToLowerInvariant();
        var tags = new List<SynergyTag>();

        if (IsAristocrats(text)) tags.Add(SynergyTag.Aristocrats);
        if (IsTokens(text)) tags.Add(SynergyTag.Tokens);
        if (IsLifegain(text)) tags.Add(SynergyTag.Lifegain);
        if (IsGraveyardRecursion(text)) tags.Add(SynergyTag.GraveyardRecursion);
        if (IsSpellsMatter(text)) tags.Add(SynergyTag.SpellsMatter);
        if (IsPlusOneCounters(text)) tags.Add(SynergyTag.PlusOneCounters);
        if (IsArtifactsMatter(text)) tags.Add(SynergyTag.ArtifactsMatter);

        return tags.AsReadOnly();
    }

    private static bool IsAristocrats(string text) =>
        text.Contains("sacrifice a creature") ||
        text.Contains("sacrifice another creature") ||
        text.Contains("whenever a creature you control dies") ||
        text.Contains("whenever another creature you control dies");

    private static bool IsTokens(string text) => text.Contains("create") && text.Contains("token");

    private static bool IsLifegain(string text) => text.Contains("gain") && text.Contains("life");

    private static bool IsGraveyardRecursion(string text) =>
        text.Contains("from your graveyard") ||
        text.Contains("from a graveyard") ||
        text.Contains("in your graveyard");

    private static bool IsSpellsMatter(string text) =>
        text.Contains("whenever you cast an instant or sorcery") ||
        text.Contains("instant or sorcery spell");

    private static bool IsPlusOneCounters(string text) => text.Contains("+1/+1 counter");

    private static bool IsArtifactsMatter(string text) =>
        text.Contains("artifact you control") ||
        text.Contains("artifacts you control") ||
        text.Contains("whenever an artifact");
}
