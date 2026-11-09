using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IForecastingRepository
{
    // Method signature here
    Task<List<HistoricalPricePoint>> GetRecentPricesAsync(string priceArea, int count = 96);
    Task<ForecastingResponseDto> PostPrices(ForecastingResponseDto forecastingResponseDto);
}