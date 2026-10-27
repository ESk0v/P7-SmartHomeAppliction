// DTOs/EnergiDataService/DayAheadPriceRecord.cs
using System.Text.Json.Serialization;

namespace SmartHomeApplicationAPI.DTOs.EnergiDataService;

/// <summary>One 15-minute row from the DayAheadPrices dataset. Prices are per MWh.</summary>
public sealed class DayAheadPriceRecord
{
    [JsonPropertyName("TimeUTC")]
    public DateTime TimeUtc { get; init; }

    [JsonPropertyName("TimeDK")]
    public DateTime TimeDk { get; init; }

    [JsonPropertyName("PriceArea")]
    public string PriceArea { get; init; } = string.Empty;

    [JsonPropertyName("DayAheadPriceDKK")]
    public decimal? DayAheadPriceDkk { get; init; }

    [JsonPropertyName("DayAheadPriceEUR")]
    public decimal? DayAheadPriceEur { get; init; }
}