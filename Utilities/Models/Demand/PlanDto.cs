using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class PlanDTO	
	{
        public int UserDeviceId { get; set; }

        public DateTime StartTime { get; set; }

    	public DateTime EndTime { get; set; }

        public int? NextPlan { get; set; }

        public decimal EstimatedPrice { get; set; }

        public decimal EstimatedCO2Emission { get; set; }
	
	}
}