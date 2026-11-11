using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IForecastingRepository
{
    // Method signature here
    Task<DateTime?> GetLatestActualPriceTimeAsync(string priceArea);
    Task<ForecastingResponseDto> PostPrices(ForecastingResponseDto forecastingResponseDto);
}