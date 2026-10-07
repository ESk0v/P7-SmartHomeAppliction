using Microsoft.AspNetCore.Identity;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
    public class User : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
