using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MtgDeckLab.API.Controllers;
using MtgDeckLab.Application.Decks.Commands.TakeDeckVersion;
using MtgDeckLab.Application.Decks.Queries.GetDeckVersionById;
using MtgDeckLab.Application.Decks.Queries.ListDeckVersions;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.API.Tests.Integration;

public class DeckVersionsTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiWebApplicationFactory _factory;

    public DeckVersionsTests(ApiWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "Pass123!" });
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private async Task SeedCardAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var cardRepo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
        var card = new Card(
            scryfallId: Guid.NewGuid(),
            name: name,
            manaCost: "{R}",
            cmc: 1,
            colors: [],
            colorIdentity: [],
            typeLine: "Instant",
            supertypes: [],
            types: [],
            subtypes: [],
            oracleText: null,
            power: null,
            toughness: null,
            loyalty: null,
            priceUsd: 1.00m,
            priceUsdFoil: null,
            setCode: "tst");
        await cardRepo.UpsertAsync(card, CancellationToken.None);
    }

    private async Task<Guid> ImportDeckAsync(HttpClient client, string name, string decklist = "4 Shock")
    {
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = name,
            Format = "Modern",
            Decklist = decklist
        });
        return (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>())!.DeckId;
    }

    [Fact]
    public async Task TakeVersion_OwnDeck_Returns201WithVersionNumber1()
    {
        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "Versioned Deck 1");

        var response = await client.PostAsync($"/api/decks/{deckId}/versions", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TakeDeckVersionResult>(JsonOptions);
        body!.VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task TakeVersion_Twice_IncrementsVersionNumber()
    {
        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "Versioned Deck 2");

        await client.PostAsync($"/api/decks/{deckId}/versions", null);
        var response = await client.PostAsync($"/api/decks/{deckId}/versions", null);

        var body = await response.Content.ReadFromJsonAsync<TakeDeckVersionResult>(JsonOptions);
        body!.VersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task TakeVersion_AnotherUsersDeck_Returns404()
    {
        var clientA = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(clientA, "Owner Versioned Deck");

        var clientB = await AuthenticatedClientAsync();
        var response = await clientB.PostAsync($"/api/decks/{deckId}/versions", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListVersions_ReturnsNewestFirst()
    {
        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "Listed Versions Deck");

        await client.PostAsync($"/api/decks/{deckId}/versions", null);
        await client.PostAsync($"/api/decks/{deckId}/versions", null);

        var response = await client.GetAsync($"/api/decks/{deckId}/versions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DeckVersionSummary>>(JsonOptions);
        body!.Should().HaveCount(2);
        body[0].VersionNumber.Should().Be(2);
        body[1].VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task ListVersions_DeckWithNoVersions_ReturnsEmptyList()
    {
        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "No Versions Deck");

        var response = await client.GetAsync($"/api/decks/{deckId}/versions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DeckVersionSummary>>(JsonOptions);
        body!.Should().BeEmpty();
    }

    [Fact]
    public async Task ListVersions_AnotherUsersDeck_Returns404()
    {
        var clientA = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(clientA, "Protected Versions Deck");

        var clientB = await AuthenticatedClientAsync();
        var response = await clientB.GetAsync($"/api/decks/{deckId}/versions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVersion_ReturnsEntriesWithResolvedCardNames()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var cardName = $"{marker} Version Card";
        await SeedCardAsync(cardName);

        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "Version Detail Deck", $"4 {cardName}");

        var takeResp = await client.PostAsync($"/api/decks/{deckId}/versions", null);
        var version = await takeResp.Content.ReadFromJsonAsync<TakeDeckVersionResult>(JsonOptions);

        var response = await client.GetAsync($"/api/decks/{deckId}/versions/{version!.VersionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeckVersionDetail>(JsonOptions);
        body!.Entries.Select(e => e.CardName).Should().Contain(cardName);
    }

    [Fact]
    public async Task GetVersion_NonExistentVersionId_Returns404()
    {
        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "Missing Version Deck");

        var response = await client.GetAsync($"/api/decks/{deckId}/versions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
