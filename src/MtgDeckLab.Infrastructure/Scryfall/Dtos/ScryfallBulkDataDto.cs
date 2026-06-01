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

    [JsonPropertyName("download_uri")]
    public string DownloadUri { get; set; } = "";
}
