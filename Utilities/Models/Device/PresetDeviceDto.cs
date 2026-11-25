using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
    public class PresetDeviceDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Preset name is required.")]
        // TODO: CreateDeviceDto.Preset allows 50 characters; this limit silently blocks
        // editing longer preset names created through the Add Device form.
        [StringLength(50)]
        public string PresetName { get; set; } = "";

        [Range(0.01, 10.0, ErrorMessage = "Consumption must be between {1} and {2} kWh per cycle.")]
        public decimal ConsumptionInKwh { get; set; }

        [Range(0.01, 999.0)]
        public decimal? BatteryCapacityInKwh { get; set; }

        [Range(1, 1440)]
        public int? DurationInMinutes { get; set; }
    }
}
