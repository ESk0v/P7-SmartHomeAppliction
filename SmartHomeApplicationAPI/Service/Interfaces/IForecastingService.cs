using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;

public interface IForecastingService
{
    // Method signature here
    Task<ForecastingResponseDto> GenerateForecastForAreaAsync(string priceArea, int stepsAhead = 96);
    Task RunDailyForecastJobAsync();
}