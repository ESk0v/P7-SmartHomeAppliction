namespace SmartHomeApplicationAPI.DTOs.Optimization;

// One 15-minute electricity row: price in DKK/kWh, CO2 in g/kWh (null when not known)
public record EnergySlot(DateTime Time, double Price, double? Co2GramsPerKwh);
