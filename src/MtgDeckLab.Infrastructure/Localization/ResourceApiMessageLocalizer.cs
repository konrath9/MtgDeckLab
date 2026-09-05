using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Localization;

namespace MtgDeckLab.Infrastructure.Localization;

public sealed class ResourceApiMessageLocalizer : IApiMessageLocalizer
{
    private readonly IStringLocalizer<ApiMessages> _localizer;
    private readonly ILogger<ResourceApiMessageLocalizer> _logger;

    public ResourceApiMessageLocalizer(
        IStringLocalizer<ApiMessages> localizer,
        ILogger<ResourceApiMessageLocalizer> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    public string Get(string code) => Get(code, []);

    public string Get(string code, params (string Key, object Value)[] args)
    {
        var template = _localizer[code];

        if (template.ResourceNotFound)
        {
            _logger.LogWarning("No translation for API message code '{Code}'.", code);
            return code;
        }

        return MessageTemplate.Render(
            template.Value, args.ToDictionary(a => a.Key, a => a.Value));
    }
}
