namespace Utilities.Models;

public record PlanDto(
    int planId,
    int? nextPlanId,
    string planName,
    string icon,
    string colorCode,
    string deviceName, 
    DateTime startTime,
    DateTime endTime,
    DateTime windowStartTime,
    DateTime windowEndTime,
    decimal estimatedCost,
    decimal estimatedCo2Emissions,
    decimal? batteryCapacityInKwh
);
