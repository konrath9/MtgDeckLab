using FluentAssertions;
using MtgDeckLab.Engine.Parsing;

namespace MtgDeckLab.Engine.Tests.Parsing;

public class DecklistParserTests
{
    private readonly DecklistParser _parser = new();

    [Fact]
    public void Parse_SimpleEntry_ReturnsCorrectEntry()
    {
        var result = _parser.Parse("4 Lightning Bolt");

        result.HasErrors.Should().BeFalse();
        result.Entries.Should().HaveCount(1);
        result.Entries[0].Quantity.Should().Be(4);
        result.Entries[0].CardName.Should().Be("Lightning Bolt");
        result.Entries[0].IsCommander.Should().BeFalse();
        result.Entries[0].IsSideboard.Should().BeFalse();
    }

    [Fact]
    public void Parse_QuantityWithX_ParsesCorrectly()
    {
        var result = _parser.Parse("4x Lightning Bolt");

        result.Entries.Should().HaveCount(1);
        result.Entries[0].Quantity.Should().Be(4);
        result.Entries[0].CardName.Should().Be("Lightning Bolt");
    }

    [Fact]
    public void Parse_CommanderTagHash_SetsIsCommander()
    {
        var result = _parser.Parse("1 Sol Ring #Commander");

        result.HasErrors.Should().BeFalse();
        result.Entries[0].IsCommander.Should().BeTrue();
        result.Entries[0].CardName.Should().Be("Sol Ring");
    }

    [Fact]
    public void Parse_CommanderTagStar_SetsIsCommander()
    {
        var result = _parser.Parse("1 Atraxa, Praetors' Voice *Commander*");

        result.HasErrors.Should().BeFalse();
        result.Entries[0].IsCommander.Should().BeTrue();
        result.Entries[0].CardName.Should().Be("Atraxa, Praetors' Voice");
    }

    [Fact]
    public void Parse_SideboardPrefix_SetsIsSideboard()
    {
        var result = _parser.Parse("SB: 2 Duress");

        result.HasErrors.Should().BeFalse();
        result.Entries[0].IsSideboard.Should().BeTrue();
        result.Entries[0].Quantity.Should().Be(2);
        result.Entries[0].CardName.Should().Be("Duress");
    }

    [Fact]
    public void Parse_SideboardSection_AllEntriesAfterHeaderAreSideboard()
    {
        var decklist = """
            4 Lightning Bolt
            // Sideboard
            2 Duress
            1 Thoughtseize
            """;

        var result = _parser.Parse(decklist);

        result.HasErrors.Should().BeFalse();
        result.Entries.Should().HaveCount(3);
        result.Entries[0].IsSideboard.Should().BeFalse();
        result.Entries[1].IsSideboard.Should().BeTrue();
        result.Entries[2].IsSideboard.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithSetCodeAndCollector_ExtractsBoth()
    {
        var result = _parser.Parse("4 Lightning Bolt (2ED) 161");

        result.HasErrors.Should().BeFalse();
        result.Entries[0].CardName.Should().Be("Lightning Bolt");
        result.Entries[0].SetCode.Should().Be("2ED");
        result.Entries[0].CollectorNumber.Should().Be(161);
    }

    [Fact]
    public void Parse_WithSetCodeOnly_ExtractsSetCode()
    {
        var result = _parser.Parse("4 Lightning Bolt (M21)");

        result.Entries[0].SetCode.Should().Be("M21");
        result.Entries[0].CollectorNumber.Should().BeNull();
    }

    [Fact]
    public void Parse_EmptyLines_AreSkipped()
    {
        var decklist = """
            4 Lightning Bolt

            2 Counterspell

            """;

        var result = _parser.Parse(decklist);

        result.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_SectionComments_AreSkipped()
    {
        var decklist = """
            // Creatures
            4 Birds of Paradise
            // Instants
            2 Counterspell
            """;

        var result = _parser.Parse(decklist);

        result.Entries.Should().HaveCount(2);
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void Parse_InvalidLine_AppearsInErrors()
    {
        var result = _parser.Parse("not a valid entry");

        result.HasErrors.Should().BeTrue();
        result.Entries.Should().BeEmpty();
        result.Errors.Should().Contain("not a valid entry");
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmpty()
    {
        var result = _parser.Parse(string.Empty);

        result.Entries.Should().BeEmpty();
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void Parse_CommanderDeck_ParsesAllZones()
    {
        var decklist = """
            1 Atraxa, Praetors' Voice #Commander
            1 Sol Ring
            38 Forest
            // Sideboard
            1 Beast Within
            """;

        var result = _parser.Parse(decklist);

        result.HasErrors.Should().BeFalse();
        result.Entries.Should().HaveCount(4);
        result.Entries[0].IsCommander.Should().BeTrue();
        result.Entries[0].CardName.Should().Be("Atraxa, Praetors' Voice");
        result.Entries[3].IsSideboard.Should().BeTrue();
    }

    [Fact]
    public void Parse_CardWithCommaInName_ParsesCorrectly()
    {
        var result = _parser.Parse("1 Korvold, Fae-Cursed King");

        result.HasErrors.Should().BeFalse();
        result.Entries[0].CardName.Should().Be("Korvold, Fae-Cursed King");
    }

    [Fact]
    public void Parse_MultipleErrors_CollectsAll()
    {
        var decklist = """
            4 Lightning Bolt
            bad line one
            bad line two
            2 Counterspell
            """;

        var result = _parser.Parse(decklist);

        result.Entries.Should().HaveCount(2);
        result.Errors.Should().HaveCount(2);
    }
}
