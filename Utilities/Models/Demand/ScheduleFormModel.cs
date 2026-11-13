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

		[Required(ErrorMessage = "Please select a priority.")]
		public string Priority { get; set; } = "Cheapest";

    	[StringLength(100)]
        public string PlanName { get; set; } = string.Empty;

        public int Start { get; set; } = 0;

        public int End { get; set; } = 0;

	}
}