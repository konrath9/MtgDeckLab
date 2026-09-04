namespace MtgDeckLab.Engine.Analysis;

// Distribuição hipergeométrica pura — sem terminologia de Magic aqui de propósito, é usada tanto
// pra mão inicial quanto pra "por turno" (ver ManaBaseAnalyzer). Usa double internamente pra evitar
// overflow de fatoriais com deck de 60-99 cartas (C(99,7) já estoura long) — o resultado final é
// sempre uma probabilidade em [0,1], convertida pra decimal na saída.
public static class HypergeometricCalculator
{
    // P(exatamente k sucessos numa amostra de sampleSize, sem reposição, de uma população
    // populationSize com successStates sucessos).
    public static decimal ProbabilityExactly(int populationSize, int successStates, int sampleSize, int k)
    {
        if (populationSize <= 0 || sampleSize <= 0 || sampleSize > populationSize) return 0m;
        if (k < 0 || k > successStates || k > sampleSize) return 0m;

        var failuresNeeded = sampleSize - k;
        var failureStates = populationSize - successStates;
        if (failuresNeeded < 0 || failuresNeeded > failureStates) return 0m;

        var probability =
            Combinations(successStates, k) *
            Combinations(failureStates, failuresNeeded) /
            Combinations(populationSize, sampleSize);

        return (decimal)Math.Clamp(probability, 0d, 1d);
    }

    // P(pelo menos k sucessos) — soma de ProbabilityExactly de k até o máximo possível.
    public static decimal ProbabilityAtLeast(int populationSize, int successStates, int sampleSize, int k)
    {
        var max = Math.Min(successStates, sampleSize);
        decimal sum = 0m;
        for (var i = Math.Max(k, 0); i <= max; i++)
            sum += ProbabilityExactly(populationSize, successStates, sampleSize, i);
        return Math.Clamp(sum, 0m, 1m);
    }

    // C(n, k) via forma multiplicativa (cancela termos incrementalmente) — evita calcular n!
    // diretamente, que estoura double pra n razoavelmente pequeno (>170).
    private static double Combinations(int n, int k)
    {
        if (k < 0 || k > n) return 0d;
        k = Math.Min(k, n - k);

        var result = 1d;
        for (var i = 0; i < k; i++)
            result = result * (n - i) / (i + 1);

        return result;
    }
}
