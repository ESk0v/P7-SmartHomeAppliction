namespace Utilities.Models;

public record DeviceDto (
    string Name,
    string Category,
    string Preset,
    decimal Consumption,  // in kwh
    int Duration        // in minutes
);