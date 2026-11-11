namespace Utilities.Models;

public record PlanDto(
    int planId,
    int? nextPlanId,
    string planName,
    string icon,
    string colorCode,
    string deviceName, 
    string deviceCategory,
    string optimizationObjective,
    int? chargeStopPercentage,
    int? presetDurationInMinutes,
    DateTime startTime,
    DateTime endTime,
    DateTime windowStartTime,
    DateTime windowEndTime,
    decimal estimatedCost,
    decimal estimatedCo2Emissions,
    decimal estimatedUnscheduledPrice,
    decimal estimatedUnscheduledCo2Emissions,
    decimal? batteryCapacityInKwh
);
