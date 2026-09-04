using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record RoleAvailabilityByTurn(CardRole Role, int Turn, decimal Probability);

// Simulação de Monte Carlo: embaralha o main deck `Iterations` vezes e mede estatísticas de jogo
// real (mão mantível, disponibilidade de papel por turno). Complementa o cálculo fechado de
// ManaBaseAnalyzer com métricas condicionadas a múltiplos fatores conjuntos (papel + turno) que
// não têm forma fechada simples. Resultado é estocástico — não use pra pontuar/comparar versões
// do deck (ver DeckAnalyzer, que é determinístico de propósito).
public sealed record MonteCarloSimulationResult(
    int Iterations,
    decimal KeepableHandRate,
    decimal AtLeastTwoLandsHandRate,
    IReadOnlyList<RoleAvailabilityByTurn> RoleAvailability
);
