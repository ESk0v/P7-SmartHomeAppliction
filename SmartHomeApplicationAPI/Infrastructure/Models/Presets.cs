using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
	[Table("presets")]
	public class Presets
	{
		[Key]
		[Column("id")]
		public int Id { get; set; }

		[Required]
		[MaxLength(30)]
		[Column("name")]
		public string Name { get; set; } = null!;

		[Required]
		[Column("consumption_in_kwh")]
		public decimal ConsumptionInKwh { get; set; }

		[Required]
		[Column("duration_in_minutes")]
		public int DurationInMinutes { get; set; }

		[Required]
		[Column("device_id")]
		public int DeviceId { get; set; }

		[ForeignKey(nameof(DeviceId))]
		public Devices Device { get; set; } = null!;
	}
}
