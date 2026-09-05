using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

// Strength = cópias no main deck (sem land) com essa tag / total de cópias não-land. É uma
// proxy simples de "quanto do deck empurra nessa direção", não uma probabilidade.
public sealed record SynergySignal(SynergyTag Tag, decimal Strength);

// DominantTag/DominantStrength só são preenchidos quando o sinal mais forte cobre pelo menos
// SynergyAnalyzer.DominantThreshold do deck — abaixo disso não há tema claro o bastante pra
// nomear um "arquétipo". LowSynergyWarnings só é populado junto com DominantTag.
public sealed record SynergyAnalysis(
    IReadOnlyList<SynergySignal> Signals,
    SynergyTag? DominantTag,
    decimal? DominantStrength,
    IReadOnlyList<AnalysisMessage> LowSynergyWarnings
);
