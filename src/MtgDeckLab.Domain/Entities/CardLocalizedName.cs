using MtgDeckLab.Domain.Localization;

namespace MtgDeckLab.Domain.Entities;

/// <summary>
/// Nome impresso de uma carta em um idioma que não o inglês (o inglês vive em <see cref="Card.Name"/>,
/// que continua sendo o nome canônico usado por análise, versionamento e chaves de negócio).
/// </summary>
public sealed class CardLocalizedName
{
    public Guid CardId { get; private init; }
    public string Language { get; private init; } = CardLanguage.English;
    public string Name { get; private set; } = string.Empty;
    public string? PrintedTypeLine { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core
    private CardLocalizedName() { }

    internal CardLocalizedName(Guid cardId, string language, string name, string? printedTypeLine)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Localized card name cannot be empty.", nameof(name));

        CardId = cardId;
        Language = CardLanguage.Normalize(language);
        Name = name.Trim();
        PrintedTypeLine = printedTypeLine;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal void Update(string name, string? printedTypeLine)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Localized card name cannot be empty.", nameof(name));

        Name = name.Trim();
        PrintedTypeLine = printedTypeLine;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
