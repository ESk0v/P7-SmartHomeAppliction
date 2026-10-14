namespace Utilities.Models;

public record PlanDto(
    string icon,
    string colorCode,
    string deviceName, 
    DateTime startTime,
    DateTime endTime
);