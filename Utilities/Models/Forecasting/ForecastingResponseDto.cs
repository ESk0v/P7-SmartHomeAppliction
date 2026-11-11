using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Utilities.Models
{
	public class ForecastingResponseDto	
	{
        [Required]
        [JsonPropertyName("price_area")]
        [RegularExpression("^(DK1|DK2)$")]
        public string PriceArea { get; set; } = string.Empty;

		[Required]
		[MinLength(1)]
        [JsonPropertyName("predictions")]
		public List<PredictedPricePoint> Predictions { get; set; } = new();
	}
}
