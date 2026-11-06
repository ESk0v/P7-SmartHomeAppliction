using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class CreateDeviceDto
	{
		public String UserId { get; set; } = string.Empty;

		[Required(ErrorMessage = "Device model is required.")]
		[StringLength(50)]
		public string Model { get; set; } = string.Empty;

		[Required(ErrorMessage = "Please select a category.")]
		[StringLength(20)]
		public string Category { get; set; } = string.Empty;

		[Required]
		[StringLength(20)]
		public string Preset { get; set; } = "";

		[Required(ErrorMessage = "Consumption is required.")]
		[Range(0.01, 500.0)]
		public decimal? ConsumptionInKwh { get; set; }

		[Required(ErrorMessage = "Duration is required.")]
		[Range(1, 1440)]
		public int? DurationInMinutes { get; set; }

		public ICollection<PresetDeviceDto> Presets { get; set; } = new List<PresetDeviceDto>();
    }
}