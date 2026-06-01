namespace MtgDeckLab.Domain.Exceptions;

public sealed class DeckValidationException : DomainException
{
    public IReadOnlyList<string> Errors { get; }

    public DeckValidationException(string error) : base(error)
    {
        Errors = new[] { error };
    }

    public DeckValidationException(IEnumerable<string> errors) : base("Deck validation failed.")
    {
        Errors = errors.ToList().AsReadOnly();
    }
}
