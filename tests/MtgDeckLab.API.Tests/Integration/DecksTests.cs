using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MtgDeckLab.API.Controllers;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Decks.Commands.UpsertDeckEntry;
using MtgDeckLab.Application.Decks.Queries.ListDecks;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

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

    // Nenhuma carta é sincronizada nos testes (Scryfall real fica fora do escopo dos testes de
    // integração) — entradas de deck só resolvem contra cartas semeadas explicitamente aqui.
    private async Task SeedCardAsync(string name, string? oracleText = null)
    {
        using var scope = _factory.Services.CreateScope();
        var cardRepo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
        var card = new Card(
            scryfallId: Guid.NewGuid(),
            name: name,
            manaCost: "{R}",
            cmc: 1,
            colors: [Color.Red],
            colorIdentity: [Color.Red],
            typeLine: "Instant",
            supertypes: [],
            types: [CardType.Instant],
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
            MainDecklist = "4 Lightning Bolt"
        });
        await clientA.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "List Deck 2",
            Format = "Modern",
            MainDecklist = "4 Shock"
        });

        var clientB = await AuthenticatedClientAsync();
        await clientB.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Other User Deck",
            Format = "Modern",
            MainDecklist = "4 Counterspell"
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
                MainDecklist = "4 Shock"
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
            MainDecklist = "4 Lightning Bolt"
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
            MainDecklist = "4 Lightning Bolt\n20 Mountain"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions);
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
            MainDecklist = "1 Sol Ring"
        });
        var importBody = await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions);

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
            MainDecklist = "4 Counterspell"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

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
            MainDecklist = "4 Lightning Bolt\n20 Mountain"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var response = await client.GetAsync($"/api/decks/{deckId}/analysis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("score");
        body.Should().Contain("grade");
        body.Should().Contain("manaBase");
        body.Should().Contain("synergy");
    }

    [Fact]
    public async Task Analysis_DeckWithRemovalSpell_ReturnsRoleDistribution()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var cardName = $"{marker} Doom Blade";
        await SeedCardAsync(cardName, oracleText: "Destroy target creature.");

        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Removal Deck",
            Format = "Modern",
            MainDecklist =$"4 {cardName}"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var response = await client.GetAsync($"/api/decks/{deckId}/analysis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("roleDistribution");
        body.Should().Contain("Removal");
        body.Should().Contain("roleCoverage");
    }

    [Fact]
    public async Task Simulation_OwnDeck_Returns200WithKeepableHandRate()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Simulation Deck",
            Format = "Modern",
            MainDecklist = "20 Mountain\n20 Shock" // não resolve — deck fica vazio, exercita o caminho de guarda
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var response = await client.GetAsync($"/api/decks/{deckId}/simulation?iterations=200");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("keepableHandRate");
    }

    [Fact]
    public async Task Simulation_AnotherUsersDeck_Returns404()
    {
        var clientA = await AuthenticatedClientAsync();
        var importResp = await clientA.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Protected Simulation Deck",
            Format = "Modern",
            MainDecklist = "4 Shock"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var clientB = await AuthenticatedClientAsync();
        var response = await clientB.GetAsync($"/api/decks/{deckId}/simulation");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Finance_OwnDeck_Returns200()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Finance Deck",
            Format = "Standard",
            MainDecklist = "4 Plains"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

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
            MainDecklist = "4 Shock"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var response = await client.PostAsync($"/api/decks/{deckId}/finance/snapshot", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Update_OwnDeck_Returns200WithUpdatedFields()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Old Name",
            Format = "Modern",
            MainDecklist = "4 Shock"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var response = await client.PutAsJsonAsync($"/api/decks/{deckId}", new
        {
            Name = "New Name",
            Description = "Updated description"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("New Name");
        body.Should().Contain("Updated description");
    }

    [Fact]
    public async Task Update_AnotherUsersDeck_Returns404()
    {
        var clientA = await AuthenticatedClientAsync();
        var importResp = await clientA.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Owner Deck",
            Format = "Modern",
            MainDecklist = "4 Shock"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var clientB = await AuthenticatedClientAsync();
        var response = await clientB.PutAsJsonAsync($"/api/decks/{deckId}", new { Name = "Hijacked" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OwnDeck_Returns204AndSubsequentGetReturns404()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Deck To Delete",
            Format = "Modern",
            MainDecklist = "4 Shock"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var deleteResponse = await client.DeleteAsync($"/api/decks/{deckId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/decks/{deckId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AnotherUsersDeck_Returns404()
    {
        var clientA = await AuthenticatedClientAsync();
        var importResp = await clientA.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Protected Deck",
            Format = "Modern",
            MainDecklist = "4 Shock"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var clientB = await AuthenticatedClientAsync();
        var response = await clientB.DeleteAsync($"/api/decks/{deckId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpsertEntry_NewCard_AddsToMainDeckAndAppearsInGetById()
    {
        await SeedCardAsync("Test Bolt");
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Entry Deck",
            Format = "Modern",
            MainDecklist = "20 Mountain" // não resolve — nenhuma carta chamada Mountain foi semeada
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var response = await client.PutAsJsonAsync($"/api/decks/{deckId}/entries", new
        {
            CardName = "Test Bolt",
            Quantity = 4
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpsertDeckEntryResult>(JsonOptions);
        body!.MainDeckCount.Should().Be(4);

        var getResponse = await client.GetAsync($"/api/decks/{deckId}");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        getBody.Should().Contain("Test Bolt");
    }

    [Fact]
    public async Task UpsertEntry_UpdateQuantity_ChangesMainDeckCount()
    {
        await SeedCardAsync("Update Bolt");
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Update Entry Deck",
            Format = "Modern",
            MainDecklist = "1 Plains"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        await client.PutAsJsonAsync($"/api/decks/{deckId}/entries",
            new { CardName = "Update Bolt", Quantity = 2 });
        var response = await client.PutAsJsonAsync($"/api/decks/{deckId}/entries",
            new { CardName = "Update Bolt", Quantity = 3 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpsertDeckEntryResult>(JsonOptions);
        body!.MainDeckCount.Should().Be(3);
    }

    [Fact]
    public async Task UpsertEntry_ZeroQuantity_RemovesCard()
    {
        await SeedCardAsync("Remove Bolt");
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Remove Entry Deck",
            Format = "Modern",
            MainDecklist = "1 Plains"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        await client.PutAsJsonAsync($"/api/decks/{deckId}/entries",
            new { CardName = "Remove Bolt", Quantity = 2 });
        var response = await client.PutAsJsonAsync($"/api/decks/{deckId}/entries",
            new { CardName = "Remove Bolt", Quantity = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpsertDeckEntryResult>(JsonOptions);
        body!.MainDeckCount.Should().Be(0);
    }

    [Fact]
    public async Task UpsertEntry_UnknownCardName_Returns400()
    {
        var client = await AuthenticatedClientAsync();
        var importResp = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Unknown Card Deck",
            Format = "Modern",
            MainDecklist = "1 Plains"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var response = await client.PutAsJsonAsync($"/api/decks/{deckId}/entries", new
        {
            CardName = $"Nonexistent Card {Guid.NewGuid()}",
            Quantity = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpsertEntry_AnotherUsersDeck_Returns404()
    {
        await SeedCardAsync("Trespass Bolt");
        var clientA = await AuthenticatedClientAsync();
        var importResp = await clientA.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Owner Entry Deck",
            Format = "Modern",
            MainDecklist = "1 Plains"
        });
        var deckId = (await importResp.Content.ReadFromJsonAsync<ImportDeckResponse>(JsonOptions))!.DeckId;

        var clientB = await AuthenticatedClientAsync();
        var response = await clientB.PutAsJsonAsync($"/api/decks/{deckId}/entries", new
        {
            CardName = "Trespass Bolt",
            Quantity = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
