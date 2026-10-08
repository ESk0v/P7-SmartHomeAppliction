using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
	[Table("devices")]
	public class Devices
	{
		[Key]
		[Column("id")]

		public int Id { get; set; }

		[Required]
		[MaxLength(50)]
		[Column("name")]
		public string Name { get; set; } = null!;

		[Required]
		[MaxLength(20)]
		[Column("category")]
		public string Category { get; set; } = null!;

		[Required]
		[MaxLength(30)]
		[Column("icon")]
		public string Icon { get; set; } = null!;

		[Required]
		[Column("preset_id")]
		public int PresetId { get; set; }

		[ForeignKey(nameof(PresetId))]
		public Preset Preset { get; set; } = null!;

		[Required]
		[Column("consumption_in_kwh")]
		public decimal ConsumptionInKwh { get; set; }

		[Required]
		[Column("duration_in_minutes")]
		public int DurationInMinutes { get; set; }
	}
}
