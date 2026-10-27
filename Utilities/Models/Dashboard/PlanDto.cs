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
    decimal estimatedCost,
    decimal estimatedCo2Emissions
);