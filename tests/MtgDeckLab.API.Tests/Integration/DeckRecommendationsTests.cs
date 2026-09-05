using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MtgDeckLab.API.Controllers;
using MtgDeckLab.Application.Decks.Queries.GetDeckRecommendations;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.API.Tests.Integration;

public class DeckRecommendationsTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiWebApplicationFactory _factory;

    public DeckRecommendationsTests(ApiWebApplicationFactory factory) => _factory = factory;

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

    private async Task SeedCardAsync(
        string name, Color color, string? oracleText = null, string typeLine = "Creature")
    {
        using var scope = _factory.Services.CreateScope();
        var cardRepo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
        var card = new Card(
            scryfallId: Guid.NewGuid(),
            name: name,
            manaCost: null,
            cmc: 2,
            colors: [color],
            colorIdentity: [color],
            typeLine: typeLine,
            supertypes: [],
            types: typeLine == "Land" ? [CardType.Land] : [CardType.Creature],
            subtypes: [],
            oracleText: oracleText,
            power: null,
            toughness: null,
            loyalty: null,
            priceUsd: 1.00m,
            priceUsdFoil: null,
            setCode: "tst");
        await cardRepo.UpsertAsync(card, CancellationToken.None);
    }

    private async Task<Guid> ImportDeckAsync(HttpClient client, string name, string decklist)
    {
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = name,
            Format = "Modern",
            MainDecklist = decklist
        });
        return (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;
    }

    [Fact]
    public async Task GetRecommendations_SuggestsInColorCandidates_ExcludingOffColorOnes()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var deckCreature = $"{marker} Black Creature";
        var inColorRemoval = $"{marker} Doom Blade";
        var offColorRemoval = $"{marker} Green Removal";

        await SeedCardAsync(deckCreature, Color.Black);
        await SeedCardAsync(inColorRemoval, Color.Black, "Destroy target creature.");
        await SeedCardAsync(offColorRemoval, Color.Green, "Destroy target creature.");

        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "Recs Deck", $"20 {deckCreature}");

        var response = await client.GetAsync($"/api/decks/{deckId}/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeckRecommendations>(JsonOptions);
        var removalRec = body!.Recommendations.Should().ContainSingle(r => r.Role == CardRole.Removal).Subject;
        removalRec.Candidates.Should().Contain(c => c.CardName == inColorRemoval);
        removalRec.Candidates.Should().NotContain(c => c.CardName == offColorRemoval);
    }

    [Fact]
    public async Task GetRecommendations_ExcludesCardsAlreadyInDeck()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var deckCreature = $"{marker} Black Creature";
        var ownedRemoval = $"{marker} Owned Removal";

        await SeedCardAsync(deckCreature, Color.Black);
        await SeedCardAsync(ownedRemoval, Color.Black, "Destroy target creature.");

        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "Owned Recs Deck", $"19 {deckCreature}\n1 {ownedRemoval}");

        var response = await client.GetAsync($"/api/decks/{deckId}/recommendations");

        var body = await response.Content.ReadFromJsonAsync<DeckRecommendations>(JsonOptions);
        var removalRec = body!.Recommendations.SingleOrDefault(r => r.Role == CardRole.Removal);
        removalRec?.Candidates.Should().NotContain(c => c.CardName == ownedRemoval);
    }

    [Fact]
    public async Task GetRecommendations_ExcludesLands()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var deckCreature = $"{marker} Black Creature";
        var landWithRemovalText = $"{marker} Weird Land";

        await SeedCardAsync(deckCreature, Color.Black);
        await SeedCardAsync(landWithRemovalText, Color.Black, "Destroy target creature.", typeLine: "Land");

        var client = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(client, "Land Recs Deck", $"20 {deckCreature}");

        var response = await client.GetAsync($"/api/decks/{deckId}/recommendations");

        var body = await response.Content.ReadFromJsonAsync<DeckRecommendations>(JsonOptions);
        var removalRec = body!.Recommendations.SingleOrDefault(r => r.Role == CardRole.Removal);
        removalRec?.Candidates.Should().NotContain(c => c.CardName == landWithRemovalText);
    }

    [Fact]
    public async Task GetRecommendations_AnotherUsersDeck_Returns404()
    {
        var clientA = await AuthenticatedClientAsync();
        var deckId = await ImportDeckAsync(clientA, "Protected Recs Deck", "1 Nonexistent Card");

        var clientB = await AuthenticatedClientAsync();
        var response = await clientB.GetAsync($"/api/decks/{deckId}/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
