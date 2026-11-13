namespace Utilities.Models;

public record ElectricityReadingDto(
    DateTime Time,
    decimal PricePerKwh,
    decimal? Co2EmissionGramsPerKwh
);
