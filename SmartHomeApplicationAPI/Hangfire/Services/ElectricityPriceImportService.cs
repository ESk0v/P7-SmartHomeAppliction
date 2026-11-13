
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
        var tomorrow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            DanishTimeZone).Date.AddDays(1);

        await ImportDayAsync(tomorrow);
    }

    #region Helpers

    private async Task ImportDayAsync(DateTime targetDate)
    {
        var existing = await _repository.GetRangeAsync(
            targetDate,
            targetDate.AddDays(1));

        var prices = await DownloadPricesAsync(targetDate);

        if (prices.Count == 0)
        {
            _logger.LogInformation(
                "Day-ahead prices for {Date} are not published yet.",
                targetDate);

            return;
        }

        var existingByKey = existing.ToDictionary(
            row => (row.Time, row.PriceArea));

        var newRows = new List<Electricity>();

        foreach (var price in prices)
        {
            if (existingByKey.TryGetValue(
                    (price.Time, price.PriceArea),
                    out var existingRow))
            {
                existingRow.PricePerKwh = price.PricePerKwh;
                existingRow.IsPredicted = false;
            }
            else
            {
                newRows.Add(price);
            }
        }

        await _repository.SaveAsync(newRows);

        _logger.LogInformation(
            "Day-ahead import for {Date}: {New} inserted, {Updated} updated, {Total} fetched.",
            targetDate,
            newRows.Count,
            prices.Count - newRows.Count,
            prices.Count);
    }

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
