using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MtgDeckLab.Application.Cards.Queries.SearchCards;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.API.Tests.Integration;

public class CardsTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiWebApplicationFactory _factory;

    public CardsTests(ApiWebApplicationFactory factory) => _factory = factory;

    private async Task SeedCardAsync(
        string name, string typeLine, decimal cmc, string setCode = "tst", IEnumerable<Color>? colors = null)
    {
        using var scope = _factory.Services.CreateScope();
        var cardRepo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
        var card = new Card(
            scryfallId: Guid.NewGuid(),
            name: name,
            manaCost: null,
            cmc: cmc,
            colors: colors ?? [],
            colorIdentity: colors ?? [],
            typeLine: typeLine,
            supertypes: [],
            types: [],
            subtypes: [],
            oracleText: null,
            power: null,
            toughness: null,
            loyalty: null,
            priceUsd: 1.00m,
            priceUsdFoil: null,
            setCode: setCode);
        await cardRepo.UpsertAsync(card, CancellationToken.None);
    }

    [Fact]
    public async Task Search_WithoutAuth_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/cards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_ByNameSubstring_ReturnsOnlyMatchingCards()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await SeedCardAsync($"{marker} Lightning Bolt", "Instant", 1);
        await SeedCardAsync($"{marker} Counterspell", "Instant", 2);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cards?name={marker}%20Lightning");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CardSummary>>(JsonOptions);
        body!.Items.Should().ContainSingle();
        body.Items[0].Name.Should().Be($"{marker} Lightning Bolt");
    }

    [Fact]
    public async Task Search_ByType_ReturnsOnlyMatchingCards()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await SeedCardAsync($"{marker} Grizzly Bears", "Creature — Bear", 2);
        await SeedCardAsync($"{marker} Shock", "Instant", 1);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cards?name={marker}&type=Creature");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CardSummary>>(JsonOptions);
        body!.Items.Should().ContainSingle();
        body.Items[0].Name.Should().Be($"{marker} Grizzly Bears");
    }

    [Fact]
    public async Task Search_ByCmcRange_ReturnsOnlyMatchingCards()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await SeedCardAsync($"{marker} Low Cost", "Instant", 1);
        await SeedCardAsync($"{marker} Mid Cost", "Instant", 3);
        await SeedCardAsync($"{marker} High Cost", "Instant", 6);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cards?name={marker}&minCmc=2&maxCmc=4");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CardSummary>>(JsonOptions);
        body!.Items.Should().ContainSingle();
        body.Items[0].Name.Should().Be($"{marker} Mid Cost");
    }

    [Fact]
    public async Task Search_ByColor_ReturnsOnlyMatchingCards()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await SeedCardAsync($"{marker} Lightning Bolt", "Instant", 1, colors: [Color.Red]);
        await SeedCardAsync($"{marker} Counterspell", "Instant", 2, colors: [Color.Blue]);
        await SeedCardAsync($"{marker} Boros Charm", "Instant", 2, colors: [Color.Red, Color.White]);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cards?name={marker}&colors=R");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CardSummary>>(JsonOptions);
        body!.Items.Should().HaveCount(2);
        body.Items.Select(i => i.Name).Should().BeEquivalentTo(
            $"{marker} Lightning Bolt", $"{marker} Boros Charm");
    }

    [Fact]
    public async Task Search_ByMultipleColors_RequiresAllOfThem()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await SeedCardAsync($"{marker} Lightning Bolt", "Instant", 1, colors: [Color.Red]);
        await SeedCardAsync($"{marker} Boros Charm", "Instant", 2, colors: [Color.Red, Color.White]);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cards?name={marker}&colors=R,W");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CardSummary>>(JsonOptions);
        body!.Items.Should().ContainSingle();
        body.Items[0].Name.Should().Be($"{marker} Boros Charm");
    }

    [Fact]
    public async Task Search_ByColorlessMarker_ReturnsOnlyCardsWithNoColor()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await SeedCardAsync($"{marker} Lightning Bolt", "Instant", 1, colors: [Color.Red]);
        await SeedCardAsync($"{marker} Sol Ring", "Artifact", 1, colors: []);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cards?name={marker}&colors=C");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CardSummary>>(JsonOptions);
        body!.Items.Should().ContainSingle();
        body.Items[0].Name.Should().Be($"{marker} Sol Ring");
    }

    [Fact]
    public async Task Search_RespectsPageSize()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++)
            await SeedCardAsync($"{marker} Card {i}", "Instant", 1);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cards?name={marker}&page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CardSummary>>(JsonOptions);
        body!.Items.Should().HaveCount(2);
        body.TotalCount.Should().Be(3);
    }
}
