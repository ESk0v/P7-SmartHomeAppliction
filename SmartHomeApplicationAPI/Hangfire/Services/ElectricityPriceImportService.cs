using System.Text.Json;
using Hangfire;
using SmartHomeApplicationAPI.DTOs.EnergiDataService;
using SmartHomeApplicationAPI.Hangfire.Repository;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public class ElectricityPriceImportService
    : IElectricityPriceImportService
{
    private const string ApiUrl = "https://api.energidataservice.dk/dataset/DayAheadPrices";
    private static readonly TimeZoneInfo DanishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
    
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

    public async Task StartImportAsync()
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            DanishTimeZone).Date;

        await ImportDayAsync(today);
        await ImportDayAsync(today.AddDays(1));
    }

    #region Helpers
    private async Task ImportDayAsync(DateTime targetDate)
    {
        var dayLength =
            DanishMidnightToUtc(targetDate.AddDays(1)) - DanishMidnightToUtc(targetDate);
        var expectedRecordCount = (int)(dayLength.TotalMinutes / 15) * 2;

        var storedCount = await _repository.CountForDateAsync(targetDate);

        if (storedCount >= expectedRecordCount)
        {
            return;
        }

        var prices = await DownloadPricesAsync(targetDate);

        if (prices.Count == 0)
        {
            _logger.LogInformation(
                "Day-ahead prices for {Date} are not published yet.",
                targetDate);

            return;
        }

        await _repository.UpdateInsertAsync(prices);

        _logger.LogInformation(
            "Day-ahead import for {Date}: {Count} rows upserted, {Expected} expected.",
            targetDate,
            prices.Count,
            expectedRecordCount);
    }

    private static DateTime DanishMidnightToUtc(DateTime danishDate) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(danishDate.Date, DateTimeKind.Unspecified),
            DanishTimeZone);
    
    private async Task<List<Electricity>> DownloadPricesAsync(DateTime targetDate)
    {
        var filter = "{\"PriceArea\":[\"DK1\",\"DK2\"]}";

        var url =
            $"{ApiUrl}" +
            $"?start={targetDate:yyyy-MM-dd}" +
            $"&end={targetDate.AddDays(1):yyyy-MM-dd}" +
            "&columns=TimeUTC,TimeDK,PriceArea,DayAheadPriceDKK,DayAheadPriceEUR" +
            "&limit=10000" +
            $"&filter={Uri.EscapeDataString(filter)}";

        var json = await _httpClient.GetFromJsonAsync<JsonElement>(url);

        // A missing "records" property is a real failure. An empty list only means "not published yet".
        if (!json.TryGetProperty("records", out var recordsElement))
        {
            throw new InvalidOperationException(
                "DayAheadPrices reply had no 'records' property.");
        }

        var rows = recordsElement.Deserialize<List<ElectricityImportResponse>>() ?? [];

        return rows
            .Where(record => record.DayAheadPriceDkk is not null)
            .Select(record => new Electricity
            {
                Time = record.TimeDk,
                PriceArea = record.PriceArea,
                PricePerKwh = record.DayAheadPriceDkk!.Value / 1000m,
                IsPredicted = false
            })
            .ToList();
    }
    
    #endregion

}