using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis;

// Heurísticas determinísticas sobre oracle text (sem IA). O objetivo é dar uma visão agregada
// da composição do deck (quanta rampa, remoção etc. ele tem) — não uma classificação garantida
// carta a carta. Uma carta pode ter mais de um papel (ex.: "Destroy target creature. Draw a
// card." conta em Removal e CardDraw).
public static class CardRoleClassifier
{
    private static readonly string[] CardDrawPhrases =
        ["draw a card", "draws a card", "draw two cards", "draw three cards", "draw four cards", "draw cards"];

    public static IReadOnlyList<CardRole> Classify(string? oracleText, IReadOnlyList<CardType> types)
    {
        if (string.IsNullOrWhiteSpace(oracleText) || types.Contains(CardType.Land))
            return [];

        var text = oracleText.ToLowerInvariant();
        var roles = new List<CardRole>();

        if (IsBoardWipe(text)) roles.Add(CardRole.BoardWipe);
        else if (IsRemoval(text)) roles.Add(CardRole.Removal);

        if (IsRamp(text)) roles.Add(CardRole.Ramp);
        if (IsCardDraw(text)) roles.Add(CardRole.CardDraw);
        if (IsTutor(text)) roles.Add(CardRole.Tutor);
        if (IsProtection(text)) roles.Add(CardRole.Protection);
        if (IsRecursion(text)) roles.Add(CardRole.Recursion);
        if (IsInteraction(text)) roles.Add(CardRole.Interaction);

        return roles.AsReadOnly();
    }

    private static bool IsBoardWipe(string text) =>
        text.Contains("destroy all creatures") ||
        text.Contains("each creature gets -") ||
        text.Contains("all creatures get -") ||
        (text.Contains("destroy all") && text.Contains("creature"));

    private static bool IsRemoval(string text) =>
        text.Contains("destroy target creature") ||
        text.Contains("exile target creature") ||
        text.Contains("target creature gets -") ||
        text.Contains("damage to target creature") ||
        text.Contains("target creature an opponent controls");

    private static bool IsRamp(string text) =>
        (text.Contains("search your library for a") && text.Contains("land")) ||
        text.Contains(": add {");

    private static bool IsCardDraw(string text) => CardDrawPhrases.Any(text.Contains);

    private static bool IsTutor(string text) =>
        text.Contains("search your library for") && !text.Contains("land");

    private static bool IsProtection(string text) =>
        text.Contains("hexproof") ||
        text.Contains("indestructible") ||
        text.Contains("protection from") ||
        text.Contains("prevent all damage") ||
        text.Contains("prevent the next");

    private static bool IsRecursion(string text) =>
        text.Contains("from your graveyard to your hand") ||
        text.Contains("from your graveyard to the battlefield") ||
        text.Contains("return target creature card from your graveyard") ||
        text.Contains("return target card from your graveyard");

    private static bool IsInteraction(string text) =>
        text.Contains("counter target spell") ||
        text.Contains("return target creature to its owner's hand") ||
        text.Contains("return target permanent to its owner's hand") ||
        text.Contains("fight");
}
