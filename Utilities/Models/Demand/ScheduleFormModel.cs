using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class ScheduleForm	
	{
	
		[Required(ErrorMessage = "Please select a device.")]
		[StringLength(50)]
		public string DeviceName { get; set; } = string.Empty;

        [Range(0, 100)]
		public int? ChargeStopPercentage { get; set; }

        public PresetDto? Preset { get; set; } = new();

        [Required(ErrorMessage = "Please select a time.")]
        [Range(0, 23)]
        public int? StartHour { get; set; }

        [Required(ErrorMessage = "Please select a time.")]
        [Range(0, 59)]
        public int? StartMinute { get; set; }

        [Required(ErrorMessage = "Please select a time.")]
        [Range(0, 23)]
        public int? EndHour { get; set; }

        [Required(ErrorMessage = "Please select a time.")]        
        [Range(0, 59)]
        public int? EndMinute { get; set; }

		[Required(ErrorMessage = "Please select a priority.")]
		public string Priority { get; set; } = "Cheapest";

	}
}