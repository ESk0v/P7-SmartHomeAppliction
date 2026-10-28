
using System.Text.Json.Serialization;

namespace SmartHomeApplicationAPI.DTOs.EnergiDataService;

public class Co2ImportResponse
{
    [JsonPropertyName("Minutes5UTC")]
    public DateTime Minutes5UTC { get; set; }

    [JsonPropertyName("Minutes5DK")]
    public DateTime Minutes5DK { get; set; }

    [JsonPropertyName("PriceArea")]
    public string PriceArea { get; set; } = string.Empty;

    [JsonPropertyName("CO2Emission")]
    public double? CO2Emission { get; set; }
}
