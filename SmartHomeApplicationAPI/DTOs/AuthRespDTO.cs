namespace SmartHomeApplicationAPI.DTOs
{
    // Basically says: An AuthRespDTO record consists of precise these 4 properties
    public record AuthRespDTO(string UserId, string Username, string DisplayName, string[] Roles);
}
