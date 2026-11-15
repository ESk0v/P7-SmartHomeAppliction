using Utilities.Models;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public interface IForecastingService
{
    // Method signature here
    Task<ForecastingResponseDto> GenerateForecastForAreaAsync(string priceArea, int stepsAhead = 96);
    Task RunDailyForecastJobAsync();
}