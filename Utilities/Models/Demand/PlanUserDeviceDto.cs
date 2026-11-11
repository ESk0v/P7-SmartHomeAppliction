namespace Utilities.Models;

public record PlanUserDeviceDto(
    int id,
    int deviceId,
    string deviceNickname,
    string icon,
    string color,
    string category,
    string model,
    decimal? batteryCapacityInKwh
);
