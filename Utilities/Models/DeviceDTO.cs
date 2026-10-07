using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class DeviceDTO	
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Device name is required.")]
		[StringLength(50)]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Please select a category.")]
		[StringLength(20)]
		public string Category { get; set; } = string.Empty;

		[Required]
		[StringLength(10)]
		public string Preset { get; set; } = "standard";

		[Required(ErrorMessage = "Consumption is required.")]
		[Range(0.01, 2000.0)]
		public decimal? ConsumptionInkWh { get; set; }

		[Required(ErrorMessage = "Duration is required.")]
		[Range(1, 1440)]
		public int? DurationInMinutes { get; set; }
	}
}