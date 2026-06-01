using System.Text.Json.Serialization;

namespace MtgDeckLab.Infrastructure.Scryfall.Dtos;

internal sealed class ScryfallCardDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("layout")]
    public string Layout { get; set; } = "normal";

    [JsonPropertyName("mana_cost")]
    public string? ManaCost { get; set; }

    [JsonPropertyName("cmc")]
    public decimal Cmc { get; set; }

    [JsonPropertyName("type_line")]
    public string? TypeLine { get; set; }

    [JsonPropertyName("oracle_text")]
    public string? OracleText { get; set; }

    [JsonPropertyName("colors")]
    public List<string>? Colors { get; set; }

    [JsonPropertyName("color_identity")]
    public List<string>? ColorIdentity { get; set; }

    [JsonPropertyName("power")]
    public string? Power { get; set; }

    [JsonPropertyName("toughness")]
    public string? Toughness { get; set; }

    [JsonPropertyName("loyalty")]
    public string? Loyalty { get; set; }

    [JsonPropertyName("set")]
    public string Set { get; set; } = "";

    [JsonPropertyName("prices")]
    public ScryfallPricesDto? Prices { get; set; }

    [JsonPropertyName("card_faces")]
    public List<ScryfallCardFaceDto>? CardFaces { get; set; }
}

internal sealed class ScryfallPricesDto
{
    [JsonPropertyName("usd")]
    public string? Usd { get; set; }

    [JsonPropertyName("usd_foil")]
    public string? UsdFoil { get; set; }
}

internal sealed class ScryfallCardFaceDto
{
    [JsonPropertyName("mana_cost")]
    public string? ManaCost { get; set; }

    [JsonPropertyName("type_line")]
    public string? TypeLine { get; set; }

    [JsonPropertyName("colors")]
    public List<string>? Colors { get; set; }
}
