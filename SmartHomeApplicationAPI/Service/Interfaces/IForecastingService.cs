using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;

public interface IForecastingService
{
    // Method signature here
    Task<ForecastingResponseDto> PostPrices(ForecastingResponseDto forecastingRequestDto);
}