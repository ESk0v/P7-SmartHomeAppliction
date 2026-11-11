using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Utilities.Models
{
    public class PredictedPricePoint
    {
        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("price_per_kwh")]
        public float PricePerKwh { get; set; }

        [JsonPropertyName("price_area")]
        [RegularExpression("^(DK1|DK2)$")]
        public string PriceArea { get; set; } = string.Empty;

        [JsonPropertyName("is_predicted")]
        public bool IsPredicted { get; set; } = true;

    }
}
