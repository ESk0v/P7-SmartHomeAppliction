using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
	[Table("device")]
	public class Device
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
		[MaxLength(10)]
		[Column("preset")]
		public string preset { get; set; } = "standard";

		[Required]
		[Column("consumption_in_kwh")]
		public decimal ConsumptionInKwh { get; set; }

		[Required]
		[Column("duration_in_minutes")]
		public int DurationInMinutes { get; set; }
	}
}
