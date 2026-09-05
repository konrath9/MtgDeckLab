using MediatR;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Decks.Commands.UpsertDeckEntry;

/// <summary>
/// Define a quantidade de uma carta num slot do deck (main/sideboard/commander/maybeboard).
/// Quantity = 0 remove a entrada. Reaproveita Deck.SetEntryQuantity, que já cobre
/// criar, atualizar e remover num único método.
/// </summary>
public record UpsertDeckEntryCommand(
    Guid DeckId,
    Guid UserId,
    string CardName,
    int Quantity,
    DeckSection Section = DeckSection.Main
) : IRequest<UpsertDeckEntryResult>;

public record UpsertDeckEntryResult(int MainDeckCount, int SideboardCount, int MaybeboardCount);
