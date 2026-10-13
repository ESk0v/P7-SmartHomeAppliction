using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
    [Table("user_devices")]
    public class UserDevice
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
        [Column("user_id")]
        public String UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [Column("nickname")]
        [MaxLength(50)]
        public string Nickname { get; set; } = null!;

        [Column("icon")]
        [MaxLength(20)]
        public string Icon { get; set; } = "default_icon";

        [Column("color")]
        [MaxLength(7)]
        public string Color { get; set; } = "#ea580b";

        [Column("is_favorite")]
        public bool IsFavorite { get; set; }

    }
}