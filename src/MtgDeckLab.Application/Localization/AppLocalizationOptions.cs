namespace MtgDeckLab.Application.Localization;

/// <summary>
/// Idiomas que a aplicação atende, lidos da seção <c>Localization</c> da configuração.
/// </summary>
/// <remarks>
/// Um idioma novo entra por aqui (mais um <c>.resx</c> por catálogo e um bundle no frontend):
/// nada no código conhece "pt-BR" ou "en-US" diretamente.
/// </remarks>
public sealed class AppLocalizationOptions
{
    public const string SectionName = "Localization";

    /// <summary>Cultura usada quando o cliente não pede nenhuma (ou pede uma que não atendemos).</summary>
    public string DefaultCulture { get; init; } = "en-US";

    public IReadOnlyList<string> SupportedCultures { get; init; } = ["en-US", "pt-BR"];
}
