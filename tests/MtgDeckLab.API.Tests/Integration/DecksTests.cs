using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MtgDeckLab.API.Controllers;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Decks.Queries.ListDecks;

namespace MtgDeckLab.API.Tests.Integration;

public class DecksTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiWebApplicationFactory _factory;

    public DecksTests(ApiWebApplicationFactory factory) => _factory = factory;

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

    [Fact]
    public async Task List_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/decks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnDecksOrderedByMostRecentlyUpdated()
    {
        var clientA = await AuthenticatedClientAsync();
        await clientA.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "List Deck 1",
            Format = "Modern",
            Decklist = "4 Lightning Bolt"
        });
        await clientA.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "List Deck 2",
            Format = "Modern",
            Decklist = "4 Shock"
        });

        var clientB = await AuthenticatedClientAsync();
        await clientB.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Other User Deck",
            Format = "Modern",
            Decklist = "4 Counterspell"
        });

        var response = await clientA.GetAsync("/api/decks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<DeckSummary>>(JsonOptions);
        body!.Items.Should().HaveCount(2);
        body.Items.Select(d => d.Name).Should().BeEquivalentTo("List Deck 1", "List Deck 2");
        body.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        var client = await AuthenticatedClientAsync();
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/decks/import", new
            {
                Name = $"Paged Deck {i}",
                Format = "Modern",
                Decklist = "4 Shock"
            });
        }

        var response = await client.GetAsync("/api/decks?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<DeckSummary>>(JsonOptions);
        body!.Items.Should().HaveCount(2);
        body.TotalCount.Should().Be(3);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Import_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Test Deck",
            Format = "Modern",
            Decklist = "4 Lightning Bolt"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Import_WithAuth_Returns201WithDeckId()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Burn Modern",
            Format = "Modern",
            Decklist = "4 Lightning Bolt\n20 Mountain"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ImportDeckResponse>();
        body!.DeckId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_OwnDeck_Returns200WithCorrectName()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "My Special Deck",
            Format = "Commander",
            Decklist = "1 Sol Ring"
        });
        var importBody = await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>();

        var response = await client.GetAsync($"/api/decks/{importBody!.DeckId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("My Special Deck");
    }

    [Fact]
    public async Task GetById_AnotherUsersDeck_Returns404()
    {
        // Usuário A cria um deck
        var clientA = await AuthenticatedClientAsync();
        var importResp = await clientA.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "User A Deck",
            Format = "Modern",
            Decklist = "4 Counterspell"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>())!.DeckId;

        // Usuário B tenta acessar
        var clientB = await AuthenticatedClientAsync();
        var response = await clientB.GetAsync($"/api/decks/{deckId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var client = await AuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/decks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Analysis_OwnDeck_Returns200WithScore()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Analysis Deck",
            Format = "Modern",
            Decklist = "4 Lightning Bolt\n20 Mountain"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>())!.DeckId;

        var response = await client.GetAsync($"/api/decks/{deckId}/analysis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("score");
        body.Should().Contain("grade");
    }

    [Fact]
    public async Task Finance_OwnDeck_Returns200()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Finance Deck",
            Format = "Standard",
            Decklist = "4 Plains"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>())!.DeckId;

        var response = await client.GetAsync($"/api/decks/{deckId}/finance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("totalCostUsd");
    }

    [Fact]
    public async Task TakeSnapshot_OwnDeck_Returns201()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Snapshot Deck",
            Format = "Pioneer",
            Decklist = "4 Shock"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>())!.DeckId;

        var response = await client.PostAsync($"/api/decks/{deckId}/finance/snapshot", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
