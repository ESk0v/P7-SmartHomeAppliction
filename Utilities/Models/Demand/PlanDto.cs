using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class PlanDTO	
	{
        public int userDeviceId { get; set; }

        public DateTime startTime { get; set; }

    	public DateTime endTime { get; set; }

        public DateTime? windowStartTime { get; set; }

        public DateTime? windowEndTime { get; set; }

        public int? nextPlanId { get; set; }

        public decimal estimatedCost { get; set; }

        public decimal estimatedCo2Emissions { get; set; }

        public decimal? estimatedUnscheduledCost { get; set; }

        public decimal? estimatedUnscheduledCo2Emissions { get; set; }

        public string planName { get; set; } = "Optimized Plan";

        public int? planId { get; set; }

        public string? icon { get; set; }

        public string? colorCode { get; set; }

        public string? deviceName { get; set; }

        public string? optimizationObjective { get; set; }

        public int? presetId { get; set; }

        public int? chargeStopPercentage { get; set; }
	
	}
}
