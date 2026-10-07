using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
    [Table("users")]
    public class Users
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("username")]
        public string Username { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        [Column("password")]
        public string Password { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        [Column("role")]
        [Comment("defaults to user. has to be upgraded manually")]
        public string Role { get; set; } = "user";
    }
}
