using System.Text.Json.Serialization;

namespace Utilities.Models
{
    public class HistoricalPricePoint
    {
        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("price_per_kwh")]
        public float PricePerKwh { get; set; }
    }
}

