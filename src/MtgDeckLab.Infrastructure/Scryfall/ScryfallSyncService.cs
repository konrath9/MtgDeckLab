using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Infrastructure.Scryfall.Dtos;

namespace MtgDeckLab.Infrastructure.Scryfall;

public sealed class ScryfallSyncService : IScryfallSyncService
{
    private static readonly string[] SkippedLayouts =
        ["token", "art_series", "emblem", "double_faced_token"];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<ScryfallSyncService> _logger;

    public ScryfallSyncService(HttpClient http, ILogger<ScryfallSyncService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async IAsyncEnumerable<Card> StreamOracleCardsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var downloadUri = await GetOracleBulkUriAsync(ct);
        if (string.IsNullOrEmpty(downloadUri))
        {
            _logger.LogError("Scryfall oracle_cards bulk URI not found.");
            yield break;
        }

        _logger.LogInformation("Downloading Scryfall bulk data from {Uri}", downloadUri);

        await foreach (var dto in StreamCardDtosAsync(downloadUri, ct))
        {
            if (SkippedLayouts.Contains(dto.Layout)) continue;

            var card = TryMapToCard(dto);
            if (card is not null) yield return card;
        }
    }

    private async Task<string?> GetOracleBulkUriAsync(CancellationToken ct)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ScryfallBulkDataResponse>(
                "bulk-data", JsonOpts, ct);

            return response?.Data
                .FirstOrDefault(d => d.Type == "oracle_cards")
                ?.JsonlDownloadUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Scryfall bulk-data metadata.");
            return null;
        }
    }

    private async IAsyncEnumerable<ScryfallCardDto> StreamCardDtosAsync(
        string uri,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var rawStream = await response.Content.ReadAsStreamAsync(ct);
        await using var gzipStream = new GZipStream(rawStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);

        while (await reader.ReadLineAsync(ct) is { Length: > 0 } line)
        {
            var dto = JsonSerializer.Deserialize<ScryfallCardDto>(line, JsonOpts);
            if (dto is not null) yield return dto;
        }
    }

    private Card? TryMapToCard(ScryfallCardDto dto)
    {
        try
        {
            var face = dto.CardFaces?.FirstOrDefault();

            var manaCost = dto.ManaCost ?? face?.ManaCost;
            var typeLine = dto.TypeLine ?? face?.TypeLine ?? "";
            var rawColors = dto.Colors ?? face?.Colors ?? [];
            var rawColorIdentity = dto.ColorIdentity ?? [];

            var colors = ParseColors(rawColors);
            var colorIdentity = ParseColors(rawColorIdentity);

            ParseTypeLine(typeLine, out var supertypes, out var types, out var subtypes);

            var priceUsd = TryParseDecimal(dto.Prices?.Usd);
            var priceUsdFoil = TryParseDecimal(dto.Prices?.UsdFoil);

            return new Card(
                dto.Id,
                dto.Name,
                manaCost,
                dto.Cmc,
                colors,
                colorIdentity,
                typeLine,
                supertypes,
                types,
                subtypes,
                dto.OracleText,
                dto.Power,
                dto.Toughness,
                dto.Loyalty,
                priceUsd,
                priceUsdFoil,
                dto.Set
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to map Scryfall card '{Name}' ({Id}).", dto.Name, dto.Id);
            return null;
        }
    }

    private static List<Color> ParseColors(IEnumerable<string> codes) =>
        codes
            .Select(c => c switch
            {
                "W" => (Color?)Color.White,
                "U" => Color.Blue,
                "B" => Color.Black,
                "R" => Color.Red,
                "G" => Color.Green,
                _ => null
            })
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToList();

    private static void ParseTypeLine(
        string typeLine,
        out List<CardSuperType> supertypes,
        out List<CardType> types,
        out List<string> subtypes)
    {
        supertypes = [];
        types = [];
        subtypes = [];

        // Split cards use " // " — take only the first face
        var line = typeLine.Split("//")[0].Trim();

        // Em-dash separates main types from subtypes
        var parts = line.Split('—', 2);
        var mainPart = parts[0].Trim();
        var subPart = parts.Length > 1 ? parts[1].Trim() : "";

        foreach (var word in mainPart.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse<CardSuperType>(word, ignoreCase: true, out var supertype))
                supertypes.Add(supertype);
            else if (Enum.TryParse<CardType>(word, ignoreCase: true, out var type))
                types.Add(type);
        }

        if (!string.IsNullOrWhiteSpace(subPart))
            subtypes.AddRange(subPart.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // Scryfall prices are always formatted with "." as the decimal separator regardless of
    // locale — parsing with the current culture (e.g. pt-BR, where "." is a thousands separator)
    // silently mangles values like "1.09" into 109.
    private static decimal? TryParseDecimal(string? value) =>
        value is not null && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
            ? d
            : null;
}
