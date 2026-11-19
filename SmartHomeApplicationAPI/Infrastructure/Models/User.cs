using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
    public class User : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;

        [Required, StringLength(3)]
        public string PriceArea { get; set; } = string.Empty;
    }
}
