using System.ComponentModel.DataAnnotations;

namespace SmartHomeApplicationAPI.DTOs
{
    public class LoginReqDTO
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
