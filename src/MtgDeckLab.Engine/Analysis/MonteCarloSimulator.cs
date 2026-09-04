using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

// "Mão mantível" = heurística comum de deck-testing: 2 a 5 terrenos numa mão inicial de 7 —
// fora dessa faixa, a maioria dos jogadores faz mulligan. Não modela mulligan de verdade (sem
// London mulligan, sem heurística de qualidade de não-terrenos).
public static class MonteCarloSimulator
{
    private const int OpeningHandSize = 7;
    private const int MinKeepableLands = 2;
    private const int MaxKeepableLands = 5;
    private static readonly int[] CheckpointTurns = [2, 3, 4, 5];
    private static readonly CardRole[] TrackedRoles = Enum.GetValues<CardRole>();

    private readonly record struct SimCard(bool IsLand, IReadOnlyList<CardRole> Roles);

    public static MonteCarloSimulationResult Simulate(DeckForAnalysis deck, int iterations = 10_000, int? seed = null)
    {
        var library = BuildLibrary(deck.MainDeck);
        if (library.Length < OpeningHandSize || iterations <= 0)
            return new MonteCarloSimulationResult(0, 0m, 0m, []);

        var random = seed.HasValue ? new Random(seed.Value) : new Random();

        var keepableHands = 0;
        var atLeastTwoLandsHands = 0;
        var roleHits = new Dictionary<(CardRole Role, int Turn), int>();
        foreach (var role in TrackedRoles)
            foreach (var turn in CheckpointTurns)
                roleHits[(role, turn)] = 0;

        for (var i = 0; i < iterations; i++)
        {
            Shuffle(library, random);

            var handLands = 0;
            for (var c = 0; c < OpeningHandSize; c++)
                if (library[c].IsLand) handLands++;

            if (handLands is >= MinKeepableLands and <= MaxKeepableLands) keepableHands++;
            if (handLands >= 2) atLeastTwoLandsHands++;

            var rolesSoFar = new HashSet<CardRole>();
            for (var c = 0; c < OpeningHandSize; c++)
                foreach (var role in library[c].Roles) rolesSoFar.Add(role);

            var processedUpTo = OpeningHandSize;
            foreach (var turn in CheckpointTurns)
            {
                var cardsSeen = Math.Min(OpeningHandSize + (turn - 1), library.Length);
                for (var idx = processedUpTo; idx < cardsSeen; idx++)
                    foreach (var role in library[idx].Roles) rolesSoFar.Add(role);
                processedUpTo = cardsSeen;

                foreach (var role in TrackedRoles)
                    if (rolesSoFar.Contains(role))
                        roleHits[(role, turn)]++;
            }
        }

        var roleAvailability = TrackedRoles
            .SelectMany(role => CheckpointTurns.Select(turn =>
                new RoleAvailabilityByTurn(role, turn, (decimal)roleHits[(role, turn)] / iterations)))
            .ToList();

        return new MonteCarloSimulationResult(
            iterations,
            (decimal)keepableHands / iterations,
            (decimal)atLeastTwoLandsHands / iterations,
            roleAvailability);
    }

    private static SimCard[] BuildLibrary(IEnumerable<DeckAnalysisEntry> mainDeck)
    {
        var cards = new List<SimCard>();
        foreach (var entry in mainDeck)
        {
            var card = new SimCard(entry.IsLand, entry.Roles);
            for (var i = 0; i < entry.Quantity; i++)
                cards.Add(card);
        }
        return cards.ToArray();
    }

    private static void Shuffle(SimCard[] cards, Random random)
    {
        for (var i = cards.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
