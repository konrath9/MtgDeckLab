namespace MtgDeckLab.Domain.Localization;

/// <summary>
/// Idiomas em que o nome de uma carta pode existir, usando os códigos de duas letras da própria
/// Scryfall ("en", "pt", "es", ...) — é a chave natural do dado que sincronizamos, então não
/// inventamos um enum paralelo que precisaria ser traduzido nos dois sentidos a cada sync.
/// </summary>
/// <remarks>
/// Isto é o idioma da <em>carta</em>, não o idioma da interface. A UI trabalha com culturas
/// BCP-47 ("pt-BR", "en-US"); <see cref="FromCulture"/> faz a ponte entre os dois. Para suportar
/// um idioma novo basta acrescentar a constante e listá-la em <see cref="Supported"/>.
/// </remarks>
public static class CardLanguage
{
    public const string English = "en";
    public const string Portuguese = "pt";

    /// <summary>Todos os idiomas de carta que a aplicação entende.</summary>
    public static readonly IReadOnlyList<string> Supported = [English, Portuguese];

    /// <summary>
    /// Idiomas buscados na Scryfall como tradução. Inglês fica de fora: é o nome canônico,
    /// guardado na própria tabela de cartas (<c>Card.Name</c>).
    /// </summary>
    public static readonly IReadOnlyList<string> Translatable = [Portuguese];

    /// <summary>Normaliza um código Scryfall/BCP-47 para um código de idioma de carta ("pt-BR" → "pt").</summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return English;

        var trimmed = code.Trim();
        var separator = trimmed.IndexOfAny(['-', '_']);
        var primary = (separator > 0 ? trimmed[..separator] : trimmed).ToLowerInvariant();

        return primary;
    }

    public static bool IsSupported(string? code) => Supported.Contains(Normalize(code));

    /// <summary>
    /// Idioma de carta correspondente a uma cultura de interface, caindo para
    /// <see cref="English"/> quando a cultura não tem cartas traduzidas.
    /// </summary>
    public static string FromCulture(string? cultureName)
    {
        var normalized = Normalize(cultureName);
        return IsSupported(normalized) ? normalized : English;
    }
}
