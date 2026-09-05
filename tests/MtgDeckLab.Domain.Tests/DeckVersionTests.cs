using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Tests;

public class DeckVersionTests
{
    [Fact]
    public void Constructor_Should_SnapshotEntriesAndComputeTotalsBySection()
    {
        var deckId = Guid.NewGuid();
        var mainCard = Guid.NewGuid();
        var sideCard = Guid.NewGuid();
        var commanderCard = Guid.NewGuid();

        var version = new DeckVersion(
            deckId,
            versionNumber: 1,
            score: 82,
            grade: "B",
            entries:
            [
                (mainCard, 40, DeckSection.Main),
                (sideCard, 5, DeckSection.Sideboard),
                (commanderCard, 1, DeckSection.Commander),
            ]);

        Assert.Equal(deckId, version.DeckId);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(82, version.Score);
        Assert.Equal("B", version.Grade);
        Assert.Equal(3, version.Entries.Count);
        Assert.Equal(40, version.TotalMainDeckCards);
        Assert.Equal(5, version.TotalSideboardCards);
        Assert.All(version.Entries, e => Assert.Equal(version.Id, e.DeckVersionId));
    }

    [Fact]
    public void Constructor_Should_AllowEmptyEntries()
    {
        var version = new DeckVersion(Guid.NewGuid(), 1, 0, "F", []);

        Assert.Empty(version.Entries);
        Assert.Equal(0, version.TotalMainDeckCards);
        Assert.Equal(0, version.TotalSideboardCards);
    }
}
