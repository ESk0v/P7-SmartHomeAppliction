using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class OptimizationResultDTO	
	{  
        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
        
		[Required]
		public decimal EstimatedPrice { get; set; }

		[Required]
		public decimal EstimatedCo2Emissions { get; set; }

	}
}