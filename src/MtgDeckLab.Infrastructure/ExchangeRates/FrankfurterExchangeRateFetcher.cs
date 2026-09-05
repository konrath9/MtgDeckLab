using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Infrastructure.ExchangeRates;

/// <summary>
/// Cotação USD→BRL via <see href="https://frankfurter.dev">Frankfurter</see> — gratuita, sem chave
/// de API, dados do Banco Central Europeu. Referência diária, não cotação de mercado em tempo
/// real; adequada para exibição de preço, não para uso transacional.
/// </summary>
public sealed class FrankfurterExchangeRateFetcher : IExchangeRateFetcher
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<FrankfurterExchangeRateFetcher> _logger;

    public FrankfurterExchangeRateFetcher(HttpClient http, ILogger<FrankfurterExchangeRateFetcher> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<decimal?> FetchUsdToBrlAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<FrankfurterResponse>(
                "latest?from=USD&to=BRL", JsonOpts, ct);

            return response?.Rates.GetValueOrDefault("BRL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch USD→BRL exchange rate from Frankfurter.");
            return null;
        }
    }

    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
