using MediatR;

namespace MtgDeckLab.Application.Cards.Commands.SyncCardTranslations;

/// <summary>
/// Sincroniza os nomes das cartas nos idiomas pedidos. Sem <paramref name="Languages"/>, usa
/// todos os idiomas traduzíveis que a aplicação conhece
/// (<see cref="MtgDeckLab.Domain.Localization.CardLanguage.Translatable"/>).
/// </summary>
public record SyncCardTranslationsCommand(IReadOnlyCollection<string>? Languages = null)
    : IRequest<SyncCardTranslationsResult>;

public record SyncCardTranslationsResult(
    int ProcessedCount,
    int AppliedCount,
    int ErrorCount,
    TimeSpan Duration
);
