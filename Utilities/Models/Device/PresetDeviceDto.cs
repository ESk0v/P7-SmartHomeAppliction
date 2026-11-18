using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
    public class PresetDeviceDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Preset name is required.")]
        // TODO: CreateDeviceDto.Preset allows 50 characters; this limit silently blocks
        // editing longer preset names created through the Add Device form.
        [StringLength(10)]
        public string PresetName { get; set; } = "";

        [Range(0.01, 2000.0)]
        public decimal ConsumptionInKwh { get; set; }

        [Range(0.01, 1000000.0)]
        public decimal? BatteryCapacityInKwh { get; set; }

        [Range(1, 1440)]
        public int? DurationInMinutes { get; set; }
    }
}
