namespace Utilities.Models;

public record PlanUserDeviceDto(
    int id,
    int deviceId,
    string deviceNickname,
    string category,
    string model
);