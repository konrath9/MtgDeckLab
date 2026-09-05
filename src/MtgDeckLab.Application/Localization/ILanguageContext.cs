using System.Globalization;

namespace MtgDeckLab.Application.Localization;

/// <summary>
/// Idioma da requisição atual, resolvido uma vez na borda (header <c>Accept-Language</c>, query
/// string ou cookie) e lido daqui pelos handlers — nenhum caso de uso conhece HTTP.
/// </summary>
public interface ILanguageContext
{
    /// <summary>Cultura de interface, ex.: <c>pt-BR</c>.</summary>
    CultureInfo Culture { get; }

    /// <summary>
    /// Idioma dos <em>nomes de carta</em> correspondente (código Scryfall, ex.: <c>pt</c>) —
    /// ver <see cref="MtgDeckLab.Domain.Localization.CardLanguage"/>.
    /// </summary>
    string CardLanguage { get; }
}
