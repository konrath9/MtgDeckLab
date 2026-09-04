using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

// Contagem de cópias no main deck por CardRole (heurística, ver CardRoleClassifier). Uma carta
// sem papel detectado não entra em nenhum bucket; TotalClassified conta cópias com >=1 papel.
public sealed record RoleDistribution(IReadOnlyDictionary<CardRole, int> CardCount, int TotalClassified);
