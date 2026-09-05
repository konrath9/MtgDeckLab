using System.Text.Json.Serialization;

namespace MtgDeckLab.Infrastructure.Scryfall.Dtos;

internal sealed class ScryfallBulkDataResponse
{
    [JsonPropertyName("data")]
    public List<ScryfallBulkDataItem> Data { get; set; } = [];
}

internal sealed class ScryfallBulkDataItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    // Scryfall serves bulk data as gzip-compressed JSON Lines; the old `download_uri`
    // (plain JSON array) field no longer exists in the API response.
    [JsonPropertyName("jsonl_download_uri")]
    public string JsonlDownloadUri { get; set; } = "";
}
