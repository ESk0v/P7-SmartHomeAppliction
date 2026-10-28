using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
	public class UserDeviceDto
	{
        public int  Id { get; set; }

        public String Model { get; set; } = string.Empty;

        public String Category { get; set; } = string.Empty;

        [StringLength(50)]
	    public string Nickname { get; set; } = string.Empty;
	
        [MaxLength(20)]
        public string Icon { get; set; } = "default_icon";

        [MaxLength(7)]
        public string Color { get; set; } = "#ea580b";

        public bool IsFavorite { get; set; }

        public List<PresetDeviceDto> Presets { get; set; } = new List<PresetDeviceDto>();
    }
}