using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class CatalogueDeviceDto
	{
		public String UserId { get; set; } = string.Empty;

		public string Model { get; set; } = string.Empty;

		public string Category { get; set; } = string.Empty;

		public string Preset { get; set; } = "standard";

		public decimal? ConsumptionInKwh { get; set; }

        public int? DurationInMinutes { get; set; }
    }
}