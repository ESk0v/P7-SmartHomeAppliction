using System.ComponentModel.DataAnnotations;

namespace SmartHomeApplicationAPI.DTOs
{
    public class RegisterReqDTO
    {
        [Required]
        [Length(2, 30)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Length(8, 30)]
        public string Password {  get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        public string PasswordConfirm { get; set; } = string.Empty;

        [Required]
        [Length(2, 30)]
        public string DisplayName {  get; set; } = string.Empty;

        [Required, RegularExpression("^(DK1|DK2)$", ErrorMessage = "Select DK1 or DK2.")]
        public string PriceArea { get; set; } = string.Empty;
    }
}
