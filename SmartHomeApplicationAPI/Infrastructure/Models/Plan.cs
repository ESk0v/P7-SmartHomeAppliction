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

        //foreign key to UserDevice.Id
		[Required]
		[Column("user_device_id")]
        public int UserDeviceId { get; set; }

		[ForeignKey(nameof(UserDeviceId))]
        public UserDevice UserDevice { get; set; } = null!;

		[Required]
        [Column("start_time")]
        public DateTime StartTime { get; set; }

		[Required]
		[Column("end_time")]
		public DateTime EndTime { get; set; }

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
		[Column("estimated_co2_emissions_in_percent")]
		public decimal EstimatedCo2Emissions { get; set; }

    }
}
