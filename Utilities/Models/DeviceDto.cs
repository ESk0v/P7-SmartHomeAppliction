namespace Utilities.Models;

public record DeviceDto (
    string Name,
    string Category,
    string Icon,
    int PresetId,
    
    decimal Consumption,  // in kwh
    int Duration        // in minutes
);