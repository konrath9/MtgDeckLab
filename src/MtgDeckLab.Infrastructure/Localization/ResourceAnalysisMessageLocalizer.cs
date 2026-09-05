using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Infrastructure.Localization;

/// <summary>
/// Renderiza <see cref="AnalysisMessage"/> usando os catálogos .resx e a cultura da requisição.
/// </summary>
public sealed class ResourceAnalysisMessageLocalizer : IAnalysisMessageLocalizer
{
    private readonly IStringLocalizer<AnalysisMessages> _localizer;
    private readonly ILogger<ResourceAnalysisMessageLocalizer> _logger;

    public ResourceAnalysisMessageLocalizer(
        IStringLocalizer<AnalysisMessages> localizer,
        ILogger<ResourceAnalysisMessageLocalizer> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    public LocalizedMessage Localize(AnalysisMessage message)
    {
        var template = _localizer[message.Code];

        if (template.ResourceNotFound)
        {
            // Sem tradução, devolvemos o próprio código: a resposta continua válida e o buraco no
            // catálogo fica visível (no log e na tela) em vez de virar uma frase vazia.
            _logger.LogWarning("No translation for analysis message code '{Code}'.", message.Code);
            return new LocalizedMessage(message.Code, message.Code, message.Args);
        }

        var text = MessageTemplate.Render(template.Value, message.Args, FormatArgument);
        return new LocalizedMessage(message.Code, text, message.Args);
    }

    public IReadOnlyList<LocalizedMessage> LocalizeAll(IEnumerable<AnalysisMessage> messages) =>
        messages.Select(Localize).ToList();

    // Enums do domínio (papel da carta, tema de sinergia) também são traduzidos — o Engine manda
    // o valor, nunca o rótulo.
    private string FormatArgument(object value) =>
        value is Enum enumValue ? LocalizeEnum(enumValue) : MessageTemplate.FormatValue(value);

    private string LocalizeEnum(Enum value)
    {
        var localized = _localizer[$"enum.{value.GetType().Name}.{value}"];
        return localized.ResourceNotFound ? value.ToString() : localized.Value;
    }
}
