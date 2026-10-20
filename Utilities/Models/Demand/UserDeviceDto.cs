namespace Utilities.Models;

public record UserDeviceDto(
    int id,
    int deviceId,
    string deviceNickname,
    string category,
    string model
);