using Utilities.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public interface IForecastingRepository
{
    // Method signature here
    Task<DateTime?> GetLatestActualPriceTimeAsync(string priceArea);
    Task<ForecastingResponseDto> PostPrices(ForecastingResponseDto forecastingResponseDto);
}