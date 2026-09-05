using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MtgDeckLab.Application.ExchangeRates.Queries.GetExchangeRate;

namespace MtgDeckLab.API.Tests.Integration;

public class ExchangeRateTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExchangeRateTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_WithoutAuth_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/exchange-rate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_BeforeAnySyncRan_ReturnsNullFields()
    {
        // O agendamento fica desligado nos testes (ScheduledSyncEnabled=false no factory) e nada
        // aqui força um sync manual — o cache começa vazio, e é assim que a resposta precisa se
        // comportar num deploy recém-subido, antes do primeiro sync completar.
        var client = _factory.CreateClient();
        var result = await client.GetFromJsonAsync<ExchangeRateResponse>("/api/exchange-rate");

        result.Should().NotBeNull();
        result!.UsdToBrl.Should().BeNull();
        result.AsOf.Should().BeNull();
    }
}
