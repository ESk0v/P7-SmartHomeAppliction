namespace Utilities.Models;

public sealed record SchedulingInputs(
    string? DeviceId,
    DateTime Date,
    int Start,
    int End,
    int? PresetId,
    int? ChargeStopPercentage,
    string Priority,
    string Area
);
