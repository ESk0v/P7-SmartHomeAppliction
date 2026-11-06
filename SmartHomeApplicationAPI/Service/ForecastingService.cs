using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;
namespace SmartHomeApplicationAPI.Service;

public class ForecastingService : IForecastingService
{
    private readonly IForecastingRepository _repository;

    public ForecastingService(IForecastingRepository repository)
    {
        _repository = repository;
    }

    // Method implemtentation here
    public async Task<ForecastingResponseDto> PostPrices(ForecastingResponseDto respDto)
    {
        return await _repository.PostPrices(respDto);
    }
}