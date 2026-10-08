namespace Utilities.Models;

public record PlanDto(
    string colorCode,
    string deviceName, 
    DateTime startTime,
    DateTime endTime
);