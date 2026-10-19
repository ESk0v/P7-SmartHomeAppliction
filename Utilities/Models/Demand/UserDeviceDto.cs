namespace Utilities.Models;

public record UserDeviceDto(
    int id,
    string deviceNickname,
    string category,
    string model
);