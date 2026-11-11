using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Utilities.Models
{
	public class ForecastingRequestDto	
	{
		[StringLength(4)]
		[Required]
		[JsonPropertyName("price_area")]
		[RegularExpression("^(DK1|DK2)$")]
		public string PriceArea { get; set; } = string.Empty;

		[Range(1, 288)]
		[JsonPropertyName("steps_ahead")]
		public int StepsAhead { get; set; } = 96;

		[Required]
		[MinLength(1)]
		[JsonPropertyName("recent_prices")]
		public List<HistoricalPricePoint> HistoricalPrices { get; set; } = new();
	}
}
