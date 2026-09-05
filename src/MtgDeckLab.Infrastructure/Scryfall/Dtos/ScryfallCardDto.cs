using System.Text.Json.Serialization;

namespace MtgDeckLab.Infrastructure.Scryfall.Dtos;

internal sealed class ScryfallCardDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    // Identidade da carta independente de impressão E de idioma — é por ela que as traduções
    // vindas do bulk multilíngue casam com a linha sincronizada do bulk em inglês.
    [JsonPropertyName("oracle_id")]
    public Guid OracleId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Idioma desta impressão (código Scryfall: "en", "pt", "es", ...).</summary>
    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "en";

    /// <summary>Nome como impresso na carta neste idioma. Ausente em impressões em inglês.</summary>
    [JsonPropertyName("printed_name")]
    public string? PrintedName { get; set; }

    [JsonPropertyName("printed_type_line")]
    public string? PrintedTypeLine { get; set; }

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

    // Cartas de duas faces traduzidas trazem o nome impresso por face; o topo do objeto não tem
    // printed_name nesse caso.
    [JsonPropertyName("printed_name")]
    public string? PrintedName { get; set; }

    [JsonPropertyName("printed_type_line")]
    public string? PrintedTypeLine { get; set; }
}
