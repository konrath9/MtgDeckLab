using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Domain.Localization;

namespace MtgDeckLab.API.Controllers;

/// <summary>
/// Idiomas atendidos pela API. O frontend consulta isto para montar o seletor de idioma em vez de
/// manter a própria lista fixa — assim um idioma novo aparece na UI só configurando o servidor.
/// </summary>
[ApiController]
[Route("api/languages")]
public class LanguagesController : ControllerBase
{
    private readonly AppLocalizationOptions _options;
    private readonly ILanguageContext _language;

    public LanguagesController(AppLocalizationOptions options, ILanguageContext language)
    {
        _options = options;
        _language = language;
    }

    /// <summary>
    /// Lista as culturas suportadas, a padrão e a que foi resolvida para esta requisição
    /// (a partir de <c>?lang=</c>, do cookie de cultura ou do header <c>Accept-Language</c>).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SupportedLanguagesResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var supported = _options.SupportedCultures
            .Select(c => new SupportedLanguage(c, CardLanguage.FromCulture(c)))
            .ToList();

        return Ok(new SupportedLanguagesResponse(
            _options.DefaultCulture, _language.Culture.Name, supported));
    }
}

public record SupportedLanguagesResponse(
    string DefaultCulture,
    string CurrentCulture,
    IReadOnlyList<SupportedLanguage> Supported
);

/// <param name="Culture">Cultura de interface (BCP-47), ex.: <c>pt-BR</c>.</param>
/// <param name="CardLanguage">Idioma dos nomes de carta usado nessa cultura, ex.: <c>pt</c>.</param>
public record SupportedLanguage(string Culture, string CardLanguage);
