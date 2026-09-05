using FluentAssertions;
using MtgDeckLab.Domain.Enums;
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
        result.Entries[0].Section.Should().Be(DeckSection.Main);
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
    public void Parse_CommanderTagHash_SetsCommanderSection()
    {
        var result = _parser.Parse("1 Sol Ring #Commander");

        result.HasErrors.Should().BeFalse();
        result.Entries[0].Section.Should().Be(DeckSection.Commander);
        result.Entries[0].CardName.Should().Be("Sol Ring");
    }

    [Fact]
    public void Parse_CommanderTagStar_SetsCommanderSection()
    {
        var result = _parser.Parse("1 Atraxa, Praetors' Voice *Commander*");

        result.HasErrors.Should().BeFalse();
        result.Entries[0].Section.Should().Be(DeckSection.Commander);
        result.Entries[0].CardName.Should().Be("Atraxa, Praetors' Voice");
    }

    [Fact]
    public void Parse_SideboardPrefix_SetsSideboardSection()
    {
        var result = _parser.Parse("SB: 2 Duress");

        result.HasErrors.Should().BeFalse();
        result.Entries[0].Section.Should().Be(DeckSection.Sideboard);
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
        result.Entries[0].Section.Should().Be(DeckSection.Main);
        result.Entries[1].Section.Should().Be(DeckSection.Sideboard);
        result.Entries[2].Section.Should().Be(DeckSection.Sideboard);
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
        result.Entries[0].Section.Should().Be(DeckSection.Commander);
        result.Entries[0].CardName.Should().Be("Atraxa, Praetors' Voice");
        result.Entries[3].Section.Should().Be(DeckSection.Sideboard);
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

    [Fact]
    public void Parse_DefaultSection_AppliesToUntaggedLines()
    {
        var result = _parser.Parse("1 Sol Ring", DeckSection.Maybeboard);

        result.Entries[0].Section.Should().Be(DeckSection.Maybeboard);
    }

    [Fact]
    public void Parse_InlineCommanderTag_OverridesDefaultSection()
    {
        // Someone pastes a full multi-section export into the Main box — the inline tag wins.
        var result = _parser.Parse("1 Atraxa, Praetors' Voice #Commander", DeckSection.Main);

        result.Entries[0].Section.Should().Be(DeckSection.Commander);
    }
}
