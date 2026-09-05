using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Parsing;

public sealed record ParsedEntry(
    int Quantity,
    string CardName,
    DeckSection Section,
    string? SetCode,
    int? CollectorNumber
);
