using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class CreateDeviceDto : IValidatableObject
	{
		public string UserId { get; set; } = string.Empty;

		[Required(ErrorMessage = "Device model is required.")]
		[StringLength(50)]
		public string Model { get; set; } = string.Empty;

		[Required(ErrorMessage = "Please select a category.")]
		[StringLength(20)]
		public string Category { get; set; } = string.Empty;

		[Range(0.01, 1000000.0)]
		public decimal? BatteryCapacityInKwh { get; set; }

		[Required]
		[StringLength(50)]
		public string Preset { get; set; } = "";

		[Required(ErrorMessage = "Consumption is required.")]
		[Range(0.01, 500.0)]
		public decimal? ConsumptionInKwh { get; set; }

		[Range(1, 1440)]
		public int? DurationInMinutes { get; set; }

		public ICollection<PresetDeviceDto> Presets { get; set; } = new List<PresetDeviceDto>();

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!string.IsNullOrEmpty(Category) && Category is not ("Interruptible" or "Non-Interruptible"))
			{
				yield return new ValidationResult("Select a valid category.", [nameof(Category)]);
				yield break;
			}

			if (Category == "Interruptible" && BatteryCapacityInKwh is null)
				yield return new ValidationResult("Battery capacity is required for interruptible devices.", [nameof(BatteryCapacityInKwh)]);

			if (Category == "Non-Interruptible" && DurationInMinutes is null)
				yield return new ValidationResult("Duration is required.", [nameof(DurationInMinutes)]);
		}
	}
}
