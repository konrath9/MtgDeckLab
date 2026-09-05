using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Domain.Localization;
using MtgDeckLab.Infrastructure.Scryfall.Dtos;

namespace MtgDeckLab.Infrastructure.Scryfall;

public sealed class ScryfallSyncService : IScryfallSyncService
{
    // "oracle_cards": uma linha por carta, sempre em inglês — a base da tabela de cartas.
    private const string OracleBulkType = "oracle_cards";

    // "all_cards": toda impressão em todo idioma. É o único bulk que traz nome traduzido, e o
    // preço disso é o tamanho (alguns GB). Por isso as traduções são um sync separado, com seu
    // próprio agendamento — não algo que roda junto do sync de cartas.
    private const string AllCardsBulkType = "all_cards";

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
        var downloadUri = await GetBulkUriAsync(OracleBulkType, ct);
        if (string.IsNullOrEmpty(downloadUri))
        {
            _logger.LogError("Scryfall {BulkType} bulk URI not found.", OracleBulkType);
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

    public async IAsyncEnumerable<CardTranslation> StreamCardTranslationsAsync(
        IReadOnlyCollection<string> languages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var wanted = languages
            .Select(CardLanguage.Normalize)
            .Where(l => l != CardLanguage.English)
            .ToHashSet();

        if (wanted.Count == 0) yield break;

        var downloadUri = await GetBulkUriAsync(AllCardsBulkType, ct);
        if (string.IsNullOrEmpty(downloadUri))
        {
            _logger.LogError("Scryfall {BulkType} bulk URI not found.", AllCardsBulkType);
            yield break;
        }

        _logger.LogInformation(
            "Downloading Scryfall multilingual bulk data from {Uri} for languages {Languages}.",
            downloadUri, string.Join(", ", wanted));

        // A mesma carta reaparece a cada reimpressão; só a primeira ocorrência por (oracle, idioma)
        // interessa. O set fica na casa das dezenas de milhares de entradas — barato perto de
        // materializar o arquivo inteiro.
        var seen = new HashSet<(Guid OracleId, string Language)>();

        await foreach (var dto in StreamCardDtosAsync(downloadUri, ct))
        {
            if (SkippedLayouts.Contains(dto.Layout)) continue;
            if (dto.OracleId == Guid.Empty) continue;

            var language = CardLanguage.Normalize(dto.Lang);
            if (!wanted.Contains(language)) continue;
            if (!seen.Add((dto.OracleId, language))) continue;

            var printedName = ResolvePrintedName(dto);
            if (printedName is null) continue;

            yield return new CardTranslation(
                dto.OracleId, language, printedName, ResolvePrintedTypeLine(dto));
        }
    }

    // Cartas de duas faces não trazem printed_name no topo — o nome traduzido está por face, e o
    // formato "Frente // Verso" espelha o nome canônico em inglês que já guardamos.
    private static string? ResolvePrintedName(ScryfallCardDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.PrintedName)) return dto.PrintedName;

        var faceNames = dto.CardFaces?
            .Select(f => f.PrintedName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        return faceNames is { Count: > 0 } ? string.Join(" // ", faceNames) : null;
    }

    private static string? ResolvePrintedTypeLine(ScryfallCardDto dto) =>
        dto.PrintedTypeLine ?? dto.CardFaces?.FirstOrDefault()?.PrintedTypeLine;

    private async Task<string?> GetBulkUriAsync(string bulkType, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ScryfallBulkDataResponse>(
                "bulk-data", JsonOpts, ct);

            return response?.Data
                .FirstOrDefault(d => d.Type == bulkType)
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
                dto.OracleId,
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
