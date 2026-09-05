namespace MtgDeckLab.Domain.Exceptions;

/// <summary>
/// Nenhuma carta corresponde ao nome informado, em nenhum idioma sincronizado.
/// </summary>
/// <remarks>
/// Carrega o nome buscado em vez de uma frase pronta: a mensagem que o usuário lê é montada na
/// borda, no idioma da requisição (chave <c>card.not_found</c> em <c>ApiMessages</c>).
/// </remarks>
public sealed class CardNotFoundException : DomainException
{
    public string CardName { get; }

    public CardNotFoundException(string cardName)
        : base($"Card '{cardName}' not found.")
    {
        CardName = cardName;
    }
}
