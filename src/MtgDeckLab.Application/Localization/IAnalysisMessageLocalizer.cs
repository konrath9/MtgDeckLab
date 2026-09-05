using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Localization;

/// <summary>
/// Traduz os códigos de mensagem do Engine para o idioma da requisição atual. Implementado na
/// Infrastructure sobre os catálogos <c>Resources/Localization/AnalysisMessages*.resx</c>.
/// </summary>
public interface IAnalysisMessageLocalizer
{
    LocalizedMessage Localize(AnalysisMessage message);

    IReadOnlyList<LocalizedMessage> LocalizeAll(IEnumerable<AnalysisMessage> messages);
}
