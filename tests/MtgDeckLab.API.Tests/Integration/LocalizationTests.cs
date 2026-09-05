using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MtgDeckLab.API.Controllers;
using MtgDeckLab.Application.Cards.Queries.SearchCards;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;
using MtgDeckLab.Application.Decks.Queries.GetDeckById;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Domain.Localization;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.API.Tests.Integration;

public class LocalizationTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiWebApplicationFactory _factory;

    public LocalizationTests(ApiWebApplicationFactory factory) => _factory = factory;

    private HttpClient CreateClient(string? acceptLanguage = null)
    {
        var client = _factory.CreateClient();
        if (acceptLanguage is not null)
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(acceptLanguage));
        return client;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string? acceptLanguage = null)
    {
        var client = CreateClient(acceptLanguage);
        var email = $"{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "Pass123!" });
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private async Task SeedTranslatedCardAsync(
        string englishName, string portugueseName, string typeLine = "Instant")
    {
        using var scope = _factory.Services.CreateScope();
        var cardRepo = scope.ServiceProvider.GetRequiredService<ICardRepository>();

        // O banco de teste é compartilhado pela classe e um [Theory] chama isto uma vez por caso —
        // sem esta guarda a mesma carta entraria duas vezes e a busca acharia duas linhas.
        if (await cardRepo.FindByNameAsync(englishName, CancellationToken.None) is not null) return;

        var card = new Card(
            scryfallId: Guid.NewGuid(),
            oracleId: Guid.NewGuid(),
            name: englishName,
            manaCost: "{R}",
            cmc: 1,
            colors: [Color.Red],
            colorIdentity: [Color.Red],
            typeLine: typeLine,
            supertypes: [],
            types: [CardType.Instant],
            subtypes: [],
            oracleText: null,
            power: null,
            toughness: null,
            loyalty: null,
            priceUsd: 1.00m,
            priceUsdFoil: null,
            setCode: "tst");

        card.SetLocalizedName(CardLanguage.Portuguese, portugueseName);
        await cardRepo.UpsertAsync(card, CancellationToken.None);
    }

    [Fact]
    public async Task Languages_ListsSupportedCultures_AndTheOneResolvedForTheRequest()
    {
        var client = CreateClient("pt-BR");

        var response = await client.GetFromJsonAsync<SupportedLanguagesResponse>("/api/languages", JsonOptions);

        response!.Supported.Select(s => s.Culture).Should().Contain(["en-US", "pt-BR"]);
        response.DefaultCulture.Should().Be("en-US");
        response.CurrentCulture.Should().Be("pt-BR");
        response.Supported.Single(s => s.Culture == "pt-BR").CardLanguage.Should().Be("pt");
    }

    [Fact]
    public async Task Languages_UnsupportedAcceptLanguage_FallsBackToTheDefaultCulture()
    {
        var client = CreateClient("ja-JP");

        var response = await client.GetFromJsonAsync<SupportedLanguagesResponse>("/api/languages", JsonOptions);

        response!.CurrentCulture.Should().Be("en-US");
    }

    [Theory]
    [InlineData("Relâmpago Trovejante")]
    [InlineData("Thundering Bolt")]
    public async Task SearchCards_FindsTheSameCard_ByEitherLanguage(string term)
    {
        await SeedTranslatedCardAsync("Thundering Bolt", "Relâmpago Trovejante");

        var client = CreateClient("pt-BR");
        var result = await client.GetFromJsonAsync<PagedResult<CardSummary>>(
            $"/api/cards?name={Uri.EscapeDataString(term)}", JsonOptions);

        var card = result!.Items.Should().ContainSingle().Subject;
        card.Name.Should().Be("Thundering Bolt");
        card.LocalizedName.Should().Be("Relâmpago Trovejante");
    }

    [Fact]
    public async Task SearchCards_InEnglish_DoesNotReturnALocalizedName()
    {
        await SeedTranslatedCardAsync("Searing Blaze", "Chama Escaldante");

        var client = CreateClient("en-US");
        var result = await client.GetFromJsonAsync<PagedResult<CardSummary>>(
            "/api/cards?name=Searing%20Blaze", JsonOptions);

        result!.Items.Should().ContainSingle().Which.LocalizedName.Should().BeNull();
    }

    [Fact]
    public async Task ImportDeck_AcceptsADecklistWrittenInPortuguese()
    {
        await SeedTranslatedCardAsync("Ancestral Recall", "Lembrança Ancestral");

        var client = await AuthenticatedClientAsync("pt-BR");
        var response = await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Deck em português",
            Format = "Modern",
            MainDecklist = "4 Lembrança Ancestral"
        });

        var import = await response.Content.ReadFromJsonAsync<JsonElement>();
        import.GetProperty("unresolvedCardNames").GetArrayLength().Should().Be(0);

        var deckId = import.GetProperty("deckId").GetGuid();
        var deck = await client.GetFromJsonAsync<DeckDetail>($"/api/decks/{deckId}", JsonOptions);

        var entry = deck!.Entries.Should().ContainSingle().Subject;
        // O nome canônico continua sendo o inglês; o traduzido vem ao lado, para exibição.
        entry.CardName.Should().Be("Ancestral Recall");
        entry.LocalizedName.Should().Be("Lembrança Ancestral");
    }

    [Fact]
    public async Task DeckAnalysis_ReturnsMessagesInTheRequestLanguage_KeepingTheCodeStable()
    {
        await SeedTranslatedCardAsync("Bolt of Judgment", "Raio do Julgamento");

        // 4 cópias num deck de 4 cartas: quebra o mínimo de 60 cartas do construído.
        var portugueseClient = await AuthenticatedClientAsync("pt-BR");
        var import = await (await portugueseClient.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Deck curto",
            Format = "Modern",
            MainDecklist = "4 Raio do Julgamento"
        })).Content.ReadFromJsonAsync<JsonElement>();

        var deckId = import.GetProperty("deckId").GetGuid();

        var ptAnalysis = await portugueseClient.GetFromJsonAsync<DeckAnalysisResponse>(
            $"/api/decks/{deckId}/analysis", JsonOptions);

        var ptError = ptAnalysis!.Validation.Errors
            .Should().Contain(e => e.Code == AnalysisMessageCodes.ConstructedMinSize).Subject;
        ptError.Text.Should().Contain("60 cartas");

        // Mesmo deck, outro idioma: o texto muda, o código não.
        var englishAnalysis = await portugueseClient.GetFromJsonAsync<DeckAnalysisResponse>(
            $"/api/decks/{deckId}/analysis?lang=en-US", JsonOptions);

        englishAnalysis!.Validation.Errors
            .Should().Contain(e => e.Code == AnalysisMessageCodes.ConstructedMinSize)
            .Which.Text.Should().Contain("60 cards");
    }

    [Fact]
    public async Task CardNotFound_IsReportedInTheRequestLanguage()
    {
        var client = await AuthenticatedClientAsync("pt-BR");
        var import = await (await client.PostAsJsonAsync("/api/decks/import", new
        {
            Name = "Deck vazio",
            Format = "Modern",
            MainDecklist = ""
        })).Content.ReadFromJsonAsync<JsonElement>();

        var deckId = import.GetProperty("deckId").GetGuid();

        var response = await client.PutAsJsonAsync($"/api/decks/{deckId}/entries", new
        {
            CardName = "Carta Que Não Existe",
            Quantity = 1,
            Section = "Main"
        });

        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("code").GetString().Should().Be("card.not_found");
        error.GetProperty("error").GetString().Should().Contain("não encontrada");
    }
}
