namespace Utilities.Models;

public record PlanDto(
    string deviceName, 
    DateTime startTime,
    DateTime endTime
);