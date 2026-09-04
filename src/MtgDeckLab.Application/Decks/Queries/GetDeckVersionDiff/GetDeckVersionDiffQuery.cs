using MediatR;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckVersionDiff;

public record GetDeckVersionDiffQuery(
    Guid DeckId, Guid FromVersionId, Guid ToVersionId, Guid UserId
) : IRequest<DeckVersionDiff?>;

public record DeckVersionDiff(
    Guid FromVersionId,
    int FromVersionNumber,
    Guid ToVersionId,
    int ToVersionNumber,
    int ScoreBefore,
    int ScoreAfter,
    int ScoreDelta,
    string GradeBefore,
    string GradeAfter,
    int TotalMainDeckCardsBefore,
    int TotalMainDeckCardsAfter,
    decimal AverageCmcBefore,
    decimal AverageCmcAfter,
    decimal TotalCostUsdBefore,
    decimal TotalCostUsdAfter,
    IReadOnlyList<DeckVersionCardChange> Added,
    IReadOnlyList<DeckVersionCardChange> Removed,
    IReadOnlyList<DeckVersionCardChange> QuantityChanged
);

public record DeckVersionCardChange(
    Guid CardId, string CardName, int QuantityBefore, int QuantityAfter, bool IsCommander, bool IsSideboard);
