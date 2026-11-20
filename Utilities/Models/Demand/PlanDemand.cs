namespace Utilities.Models;

public sealed record PlanDemand(
    SchedulingInputs Inputs,
    PlanUserDeviceDto Device,
    string Name,
    DateTime WindowStart,
    DateTime WindowEnd
);
