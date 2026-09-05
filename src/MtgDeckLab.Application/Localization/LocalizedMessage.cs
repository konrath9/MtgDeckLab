namespace MtgDeckLab.Application.Localization;

/// <summary>
/// Uma <see cref="MtgDeckLab.Engine.Analysis.Models.AnalysisMessage"/> já renderizada no idioma
/// do request.
/// </summary>
/// <remarks>
/// <see cref="Code"/> e <see cref="Args"/> continuam na resposta de propósito: um cliente que
/// prefira traduzir por conta própria (ou exibir o dado cru) não fica refém do texto que
/// mandamos, e o código é estável enquanto a frase pode mudar.
/// </remarks>
public sealed record LocalizedMessage(
    string Code,
    string Text,
    IReadOnlyDictionary<string, object> Args
);
