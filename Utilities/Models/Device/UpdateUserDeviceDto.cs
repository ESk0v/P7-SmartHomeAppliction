using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
    public class UpdateUserDeviceDto : IValidatableObject
    {
        public int Id { get; set; }

        public string Category { get; set; } = string.Empty;

        [StringLength(50)]
        [Required]
        public string Nickname { get; set; } = "";

        [MaxLength(20)]
        [Required]
        public string Icon { get; set; } = "default_icon";

        [MaxLength(7)]
        [Required]
        public string Color { get; set; } = "#ea580b";

        public bool IsFavorite { get; set; }

        [Range(0.01, 1000000.0)]
        public decimal? BatteryCapacityInKwh { get; set; }

        [Range(0.01, 2000.0)]
        public decimal? ConsumptionInKwh { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Category != "Interruptible") yield break;

            if (BatteryCapacityInKwh is null)
                yield return new ValidationResult("Battery capacity is required.", [nameof(BatteryCapacityInKwh)]);

            if (ConsumptionInKwh is null)
                yield return new ValidationResult("Consumption is required.", [nameof(ConsumptionInKwh)]);
        }
    }
}
