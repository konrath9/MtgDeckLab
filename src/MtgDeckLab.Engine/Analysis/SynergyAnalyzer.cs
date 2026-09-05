using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public static class SynergyAnalyzer
{
    // Sinal precisa cobrir pelo menos 30% do main deck (sem land) pra virar "tema dominante" —
    // abaixo disso o deck não tem uma identidade clara o bastante pra apontar um arquétipo ou
    // sinalizar cartas "fora do plano".
    private const decimal DominantThreshold = 0.30m;

    public static SynergyAnalysis Analyze(IEnumerable<DeckAnalysisEntry> mainDeckEntries)
    {
        var entries = mainDeckEntries.Where(e => !e.IsLand).ToList();
        var totalNonLand = entries.Sum(e => e.Quantity);
        if (totalNonLand == 0)
            return new SynergyAnalysis([], null, null, []);

        var counts = new Dictionary<SynergyTag, int>();
        foreach (var entry in entries)
            foreach (var tag in entry.SynergyTags)
                counts[tag] = counts.GetValueOrDefault(tag) + entry.Quantity;

        var signals = counts
            .Select(kv => new SynergySignal(kv.Key, (decimal)kv.Value / totalNonLand))
            .OrderByDescending(s => s.Strength)
            .ToList();

        var dominant = signals.FirstOrDefault();
        if (dominant is null || dominant.Strength < DominantThreshold)
            return new SynergyAnalysis(signals, null, null, []);

        // "Fora do plano" = nem tem papel funcional (ramp/removal/draw/...) nem toca em nenhuma
        // mecânica detectada — não basta "não bater na tag dominante", já que cartas genéricas
        // (remoção, mana rock) legitimamente não mencionam "sacrifice"/"token"/etc. no texto.
        var warnings = entries
            .Where(e => e.Roles.Count == 0 && e.SynergyTags.Count == 0)
            .Select(e => AnalysisMessage.Of(
                AnalysisMessageCodes.SynergyOffTheme,
                ("card", e.CardName), ("theme", dominant.Tag)))
            .ToList();

        return new SynergyAnalysis(signals, dominant.Tag, dominant.Strength, warnings);
    }
}
