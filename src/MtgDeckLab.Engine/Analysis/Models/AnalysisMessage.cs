namespace MtgDeckLab.Engine.Analysis.Models;

/// <summary>
/// Um achado da análise em forma legível por máquina: um <see cref="Code"/> estável mais os
/// valores que entram na frase.
/// </summary>
/// <remarks>
/// O Engine nunca produz prosa. Ele é determinístico e independente de idioma (é o que permite
/// versionar e diferenciar análises), então quem transforma código + argumentos em texto no
/// idioma do usuário é a camada de apresentação — ver <c>IAnalysisMessageLocalizer</c> na
/// Application. Acrescentar um idioma é acrescentar um catálogo de tradução, não mexer aqui.
/// </remarks>
public sealed record AnalysisMessage(string Code, IReadOnlyDictionary<string, object> Args)
{
    private static readonly IReadOnlyDictionary<string, object> NoArgs =
        new Dictionary<string, object>();

    public static AnalysisMessage Of(string code) => new(code, NoArgs);

    public static AnalysisMessage Of(string code, params (string Key, object Value)[] args) =>
        new(code, args.ToDictionary(a => a.Key, a => a.Value));
}
