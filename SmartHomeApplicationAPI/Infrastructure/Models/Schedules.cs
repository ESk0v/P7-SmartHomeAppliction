using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
	[Table("schedules")]
	public class Schedules
	{
		[Key]
		[Column("id")]

		public int Id { get; set; }

		// Foreign key to Users.Id
		[Required]
		[Column("user_id")]
		public int UserId { get; set; }

		[ForeignKey(nameof(UserId))]
		public Users User { get; set; } = null!;

        //foreign key to Device.Id
		[Required]
		[Column("device_id")]
        public int DeviceId { get; set; }

		[ForeignKey(nameof(DeviceId))]
        public Devices Device { get; set; } = null!;

		[Required]
        [Column("start_time")]
        public DateTime StartTime { get; set; }

		[Required]
		[Column("end_time")]
		public DateTime EndTime { get; set; }

		// If this schedule is split over two periods, point to the next/continuation schedule here.
		// Nullable because not every schedule is split.
		[Column("next_schedule_id")]
		public int? NextScheduleId { get; set; }

		[ForeignKey(nameof(NextScheduleId))]
		public Schedules? NextSchedule { get; set; }

		[Required]
		[Column("estimated_price_in_kroner", TypeName = "money")]
		public decimal EstimatedPrice { get; set; }

		[Required]
		[Column("estimated_co2_emissions_in_percent")]
		public decimal EstimatedCo2Emissions { get; set; }

		[Required]
		[MaxLength(7)]
		[Column("color")]
		public string Color { get; set; } = "#ea580b";
    }
}
