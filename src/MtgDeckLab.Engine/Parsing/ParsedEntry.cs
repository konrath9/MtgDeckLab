namespace MtgDeckLab.Engine.Parsing;

public sealed record ParsedEntry(
    int Quantity,
    string CardName,
    bool IsCommander,
    bool IsSideboard,
    string? SetCode,
    int? CollectorNumber
);
