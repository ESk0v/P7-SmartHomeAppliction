using System.Text.Json;
using Utilities.Models;
using SmartHomeApplicationAPI.Hangfire.Repository;

namespace SmartHomeApplicationAPI.Hangfire.Services;

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
        priceArea = priceArea.ToUpperInvariant();

        var targetDate = DateTime.UtcNow.Date.AddDays(2);

        var url = $"/api/forecast?area={priceArea}&mode=spot_inkl";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        var predictions = new List<PredictedPricePoint>();
        var root = doc.RootElement;

        // elpriser.org returnerer et objekt med en "days"-array
        if (root.TryGetProperty("days", out var daysElement) && daysElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var day in daysElement.EnumerateArray())
            {
                string dateStr = day.GetProperty("date").GetString()!;
                DateTime baseDate = DateTime.Parse(dateStr);

                if (baseDate.Date != targetDate)
                    continue;

                if (!day.TryGetProperty("prices", out var pricesElement) || pricesElement.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var hourObj in pricesElement.EnumerateArray())
                {
                    float price = hourObj.GetProperty("price").GetSingle();
                    int hour = hourObj.GetProperty("hour").GetInt32();

                    DateTime hourStart = baseDate.AddHours(hour);

                    // Udvid hver time til fire 15-minutters intervaller
                    for (int minute = 0; minute < 60; minute += 15)
                    {
                        var quarterTime = DateTime.SpecifyKind(hourStart.AddMinutes(minute), DateTimeKind.Unspecified);

                        // Kun medtag tider der ligger efter de seneste rigtige priser i databasen
                        predictions.Add(new PredictedPricePoint
                        {
                            Time = quarterTime,
                            PricePerKwh = price,
                            PriceArea = priceArea,
                            IsPredicted = true
                        });
                    }
                }
            }
        }

        var filteredPredictions = predictions
            .OrderBy(p => p.Time)
            .Take(stepsAhead)
            .ToList();

        var responseDto = new ForecastingResponseDto
        {
            PriceArea = priceArea,
            Predictions = filteredPredictions
        };

        return await _repository.PostPrices(responseDto);
    }

    public async Task RunDailyForecastJobAsync()
    {
        await GenerateForecastForAreaAsync("DK1", 96);
        await GenerateForecastForAreaAsync("DK2", 96);
    }
}