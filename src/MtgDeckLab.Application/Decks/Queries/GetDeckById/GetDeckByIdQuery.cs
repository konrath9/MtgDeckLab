using MediatR;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckById;

public record GetDeckByIdQuery(Guid DeckId, Guid UserId) : IRequest<DeckDetail?>;

public record DeckDetail(
    Guid Id,
    string Name,
    Format Format,
    string? Description,
    int MainDeckCount,
    int SideboardCount,
    int MaybeboardCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DeckEntryDetail> Entries
);

// CardName é sempre o nome canônico em inglês (chave de negócio, usada por importação e
// versionamento); LocalizedName é o nome impresso no idioma do usuário, nulo quando não há
// tradução — o cliente exibe LocalizedName ?? CardName.
public record DeckEntryDetail(
    Guid CardId,
    string CardName,
    string? LocalizedName,
    int Quantity,
    DeckSection Section,
    IReadOnlyList<CardType> Types,
    decimal Cmc,
    decimal? PriceUsd,
    string? ManaCost
);
