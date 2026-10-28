using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
	[Table("plan")]
	public class Plan
	{
		[Key]
		[Column("id")]
		public int Id { get; set; }

		[Required]
		[MaxLength(100)]
		[Column("name")]
		public string Name { get; set; } = null!;

        //foreign key to UserDevice.Id
		[Required]
		[Column("user_device_id")]
		public int UserDeviceId { get; set; }

		[ForeignKey(nameof(UserDeviceId))]
		public UserDevice UserDevice { get; set; } = null!;

		[Required]
		[Column("plan_start_time", TypeName = "timestamp without time zone")]
		public DateTime PlanStartTime { get; set; }

		[Required]
		[Column("plan_end_time", TypeName = "timestamp without time zone")]
		public DateTime PlanEndTime { get; set; }

		[Required]
		[Column("window_start_time", TypeName = "timestamp without time zone")]
		public DateTime WindowStartTime { get; set; }

		[Required]
		[Column("window_end_time", TypeName = "timestamp without time zone")]
		public DateTime WindowEndTime { get; set; }

		// If this schedule is split over two periods, point to the next/continuation schedule here.
		// Nullable because not every schedule is split.
		[Column("next_plan_id")]
		public int? NextPlanId { get; set; }

		[ForeignKey(nameof(NextPlanId))]
		public Plan? NextPlan { get; set; }

		[Required]
		[Column("estimated_price_in_kroner", TypeName = "money")]
		public decimal EstimatedPrice { get; set; }

		[Required]
		[Column("estimated_unscheduled_price_in_kroner", TypeName = "money")]
		public decimal EstimatedUnscheduledPrice { get; set; }

		[Required]
		[Column("estimated_co2_emissions_in_grams")]
		public decimal EstimatedCo2Emissions { get; set; }

		[Required]
		[Column("estimated_unscheduled_co2_emissions_in_grams")]
		public decimal EstimatedUnscheduledCo2Emissions { get; set; }

		[Required]
		[MaxLength(10)]
		[Column("optimization_objective")]
		public string OptimizationObjective { get; set; } = null!;

    }
}
