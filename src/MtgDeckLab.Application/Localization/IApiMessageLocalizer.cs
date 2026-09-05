namespace MtgDeckLab.Application.Localization;

/// <summary>
/// Mensagens de erro que a API devolve ao cliente (login inválido, carta não encontrada, ...),
/// no idioma da requisição.
/// </summary>
/// <remarks>
/// Os códigos são as chaves do catálogo <c>ApiMessages</c>; ver <see cref="ApiMessageCodes"/>.
/// </remarks>
public interface IApiMessageLocalizer
{
    string Get(string code);

    string Get(string code, params (string Key, object Value)[] args);
}

/// <summary>Chaves do catálogo <c>ApiMessages</c>. São contrato com os arquivos .resx.</summary>
public static class ApiMessageCodes
{
    public const string InvalidCredentials = "auth.invalid_credentials";
    public const string EmailAlreadyRegistered = "auth.email_already_registered";
    public const string CardNotFound = "card.not_found";
    public const string DeckNotFound = "deck.not_found";
}
