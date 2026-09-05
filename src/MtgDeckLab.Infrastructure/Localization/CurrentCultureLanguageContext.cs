using System.Globalization;
using MtgDeckLab.Application.Localization;
using DomainCardLanguage = MtgDeckLab.Domain.Localization.CardLanguage;

namespace MtgDeckLab.Infrastructure.Localization;

/// <summary>
/// Idioma da requisição lido de <see cref="CultureInfo.CurrentUICulture"/>, que o
/// <c>RequestLocalizationMiddleware</c> do ASP.NET Core já resolve na borda a partir do header
/// <c>Accept-Language</c>, da query string ou do cookie.
/// </summary>
/// <remarks>
/// A cultura corrente é ambiente (flui com o <c>AsyncLocal</c> da requisição), então este serviço
/// não guarda estado e pode ser singleton. Fora de uma requisição HTTP (um sync agendado, por
/// exemplo) vale a cultura padrão do processo.
/// </remarks>
public sealed class CurrentCultureLanguageContext : ILanguageContext
{
    public CultureInfo Culture => CultureInfo.CurrentUICulture;

    public string CardLanguage => DomainCardLanguage.FromCulture(CultureInfo.CurrentUICulture.Name);
}
