using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class OptimizationRequestDTO	
	{
		[StringLength(3, MinimumLength = 3)]
        public string Area { get; set; } = string.Empty;
        
        [StringLength(50)]
		public string DeviceName { get; set; } = string.Empty;

        [Range(0, 100)]
		public int? ChargeStartPercentage { get; set; }

        [Range(0, 100)]
		public int? ChargeStopPercentage { get; set; }

        public PresetDto? Preset { get; set; } = null;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

		[StringLength(20)]
		public string Priority { get; set; } = string.Empty;

	}
}