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
		[MaxLength(100)]
		[Column("model")]
		public string Model { get; set; } = null!;

		[Required]
		[MaxLength(20)]
		[Column("category")]
		public string Category { get; set; } = null!;

		ICollection<DevicePresets> DevicePresets { get; set; } = new List<DevicePresets>(); // For querying over all presets related to this device
        ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>(); // For querying over how many users have this device 
    }
}