using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
	[Table("device_presets")]
	public class DevicePresets
	{
		[Key]
		[Column("id")]
		public int Id { get; set; }

		[Required]
		[Column("device_id")]
		public int DeviceId { get; set; }

		[ForeignKey(nameof(DeviceId))]
		public Devices? Device { get; set; }

		[Required]
		[MaxLength(30)]
		[Column("preset_name")]
		public string PresetName { get; set; } = null!;

		[Required]
		[Precision(5, 3)]
		[Column("consumption_in_kwh")]
		public decimal ConsumptionInKwh { get; set; }

		[Required]
		[Column("duration_in_minutes")]
		public int DurationInMinutes { get; set; }
	}
}
