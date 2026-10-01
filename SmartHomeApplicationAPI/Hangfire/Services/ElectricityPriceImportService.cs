using System.Text.Json.Serialization;
using Hangfire;
using SmartHomeApplicationAPI.Hangfire.Repository;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public class ElectricityPriceImportService
    : IElectricityPriceImportService
{
    private const string ApiUrl =
        "https://api.energidataservice.dk/dataset/DayAheadPrices";

    private static readonly TimeZoneInfo DanishTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private readonly HttpClient _httpClient;
    private readonly IElectricityPriceImportRepository _repository;
    private readonly ILogger<ElectricityPriceImportService> _logger;

    public ElectricityPriceImportService(
        HttpClient httpClient,
        IElectricityPriceImportRepository repository,
        ILogger<ElectricityPriceImportService> logger)
    {
        _httpClient = httpClient;
        _repository = repository;
        _logger = logger;
    }

    public Task StartImportAsync()
    {
        var danishNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            DanishTimeZone);
    
        var today = danishNow.Date;
        var tomorrow = today.AddDays(1);
    
        var retryWindowEnds = today.AddHours(14);
    
        BackgroundJob.Enqueue<IElectricityPriceImportService>(
            service => service.ImportAttemptAsync(
                today,
                retryWindowEnds,
                0));
    
        BackgroundJob.Enqueue<IElectricityPriceImportService>(
            service => service.ImportAttemptAsync(
                tomorrow,
                retryWindowEnds,
                0));
    
        return Task.CompletedTask;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ImportAttemptAsync(
        DateTime targetDate,
        DateTime retryWindowEnds,
        int attempt)
    {
        var prices = await DownloadPricesAsync(targetDate);

        await _repository.UpdateInsertAsync(prices);

        var storedCount = await _repository.CountForDateAsync(targetDate);

        // DK1 and DK2 normally contain 96 quarter-hour records each.
        const int expectedRecordCount = 192;

        if (storedCount >= expectedRecordCount)
        {
            _logger.LogInformation(
                "Day-ahead import completed for {Date}. Stored {Count} records.",
                targetDate,
                storedCount);

            return;
        }

        var danishNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            DanishTimeZone);
        
        if (danishNow.AddMinutes(5) >= retryWindowEnds)
        {
            _logger.LogWarning(
                "Day-ahead import stopped for {Date}. Stored {Count} records.",
                targetDate,
                storedCount);
        
            return;
        }
        
        BackgroundJob.Schedule<IElectricityPriceImportService>(
            service => service.ImportAttemptAsync(
                targetDate,
                retryWindowEnds,
                attempt + 1),
            TimeSpan.FromMinutes(5));

        _logger.LogInformation(
            "Attempt {Attempt} scheduled for {Date}.",
            attempt + 1,
            targetDate);
    }

    private async Task<List<DayAheadPrice>> DownloadPricesAsync(
        DateTime targetDate)
    {
        var filter = "{\"PriceArea\":[\"DK1\",\"DK2\"]}";

        var url =
            $"{ApiUrl}" +
            $"?start={targetDate:yyyy-MM-dd}" +
            $"&end={targetDate.AddDays(1):yyyy-MM-dd}" +
            "&limit=10000" +
            $"&filter={Uri.EscapeDataString(filter)}";

        var response = await _httpClient.GetFromJsonAsync<ApiResponse>(url);

        if (response?.Records is null)
        {
            throw new InvalidOperationException(
                "DayAheadPrices API returned no records.");
        }

        return response.Records
            .Select(record => new DayAheadPrice
            {
                Time = record.TimeDk,
                PriceArea = record.PriceArea,
                Price = record.DayAheadPriceDkk,
                IsPredicted = false
            })
            .ToList();
    }

    private sealed class ApiResponse
    {
        [JsonPropertyName("records")]
        public List<ApiRecord> Records { get; set; } = [];
    }

    private sealed class ApiRecord
    {
        [JsonPropertyName("TimeDK")]
        public DateTime TimeDk { get; set; }

        [JsonPropertyName("PriceArea")]
        public string PriceArea { get; set; } = string.Empty;

        [JsonPropertyName("DayAheadPriceDKK")]
        public decimal DayAheadPriceDkk { get; set; }
    }
}