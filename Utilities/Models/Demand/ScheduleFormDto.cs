using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class ScheduleDTO	
	{
	
		[Required(ErrorMessage = "Please select a device.")]
		[StringLength(50)]
		public string DeviceName { get; set; } = string.Empty;

        [Range(0, 100)]
		public int? ChargeStartPercentage { get; set; }

        [Range(0, 100)]
		public int? ChargeStopPercentage { get; set; }

        [StringLength(30)]
        public string Preset { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a time.")]
        [Range(0, 24)]
        public int? StartHour { get; set; }

        [Required(ErrorMessage = "Please select a time.")]
        [Range(0, 60)]
        public int? StartMinute { get; set; }

        [Required(ErrorMessage = "Please select a time.")]
        [Range(0, 24)]
        public int? EndHour { get; set; }

        [Required(ErrorMessage = "Please select a time.")]        
        [Range(0, 60)]
        public int? EndMinute { get; set; }

		[Required(ErrorMessage = "Please select a priority.")]
		[StringLength(20)]
		public string Priority { get; set; } = string.Empty;

	}
}