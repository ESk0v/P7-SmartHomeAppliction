using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;
namespace SmartHomeApplicationAPI.Service;

public class ForecastingService : IForecastingService
{
    private readonly IForecastingRepository _repository;
    private readonly HttpClient _httpClient;

    public ForecastingService(IForecastingRepository repository, HttpClient httpClient)
    {
        _repository = repository;
        _httpClient = httpClient;
    }

    // Method implemtentation here
    public async Task<ForecastingResponseDto> GenerateForecastForAreaAsync(string priceArea, int stepsAhead = 96)
    {
        var recentPrices = await _repository.GetRecentPricesAsync(priceArea, 96);

        var requestDto = new ForecastingRequestDto
        {
            PriceArea = priceArea,
            StepsAhead = stepsAhead,
            HistoricalPrices = recentPrices
        };


        var response = await _httpClient.PostAsJsonAsync("/forecast", requestDto);
        response.EnsureSuccessStatusCode();

        var responseDto = await response.Content.ReadFromJsonAsync<ForecastingResponseDto>();
        if (responseDto is null)
        {
            throw new InvalidOperationException("Could not read the answer from forecasting python service");
        }

        return await _repository.PostPrices(responseDto);
    }

    public async Task RunDailyForecastJobAsync()
    {
        await GenerateForecastForAreaAsync("DK1", 96);
        await GenerateForecastForAreaAsync("DK2", 96);
    }
}