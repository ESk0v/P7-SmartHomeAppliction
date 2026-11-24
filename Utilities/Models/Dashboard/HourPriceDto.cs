namespace Utilities.Models;
public record HourPriceDto(
    DateTime Hour, 
    decimal Price,
    bool IsPredicted = false,
    decimal? Co2EmissionGramsPerKwh = null
);
