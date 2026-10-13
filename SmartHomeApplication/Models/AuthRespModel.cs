namespace SmartHomeApplication.Models
{
    public record AuthRespModel(string UserId, string Username, string DisplayName, string[] Roles);
}
