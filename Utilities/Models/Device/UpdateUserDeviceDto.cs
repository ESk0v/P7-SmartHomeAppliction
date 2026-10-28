using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
    public class UpdateUserDeviceDto
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Required]
        public string Nickname { get; set; } = "";

        [MaxLength(20)]
        [Required]
        public string Icon { get; set; } = "default_icon";

        [MaxLength(7)]
        [Required]
        public string Color { get; set; } = "#ea580b";

        public bool IsFavorite { get; set; }
    }
}