namespace SmartHomeApplicationAPI.DTOs
{
    public record AuthRespDTO(
        string UserId,
        string Username, 
        string DisplayName, 
        string[] Roles, 
        string PriceArea = "DK1");
}
