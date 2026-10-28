// Services/Co2ImportService.cs
using System.Text.Json;
using SmartHomeApplicationAPI.DTOs.EnergiDataService;
using SmartHomeApplicationAPI.Hangfire.Repository;
using SmartHomeApplicationAPI.Hangfire.Services;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public class Co2ImportService
    : ICo2ImportService
{
    private const string ApiUrl = "https://api.energidataservice.dk/dataset/CO2EmisProg";
    private static readonly TimeZoneInfo DanishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private readonly HttpClient _httpClient;
    private readonly ICo2ImportRepository _repository;
    private readonly ILogger<Co2ImportService> _logger;

    public Co2ImportService(
        HttpClient httpClient,
        ICo2ImportRepository repository,
        ILogger<Co2ImportService> logger)
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
        var rows = await _repository.GetRangeAsync(
            targetDate,
            targetDate.AddDays(1));

        if (rows.Count == 0)
        {
            _logger.LogInformation(
                "No price rows for {Date} yet, CO2  skipped.",
                targetDate);

            return;
        }

        // Only empty slots are filled, so the  can never overwrite a measured value.
        var emptyRows = rows
            .Where(row => row.Co2EmissionGramsPerKwh is null)
            .ToList();

        if (emptyRows.Count == 0)
        {
            return;
        }

        var readings = await DownloadAsync(targetDate);

        var slots = readings
            .Where(r => r.CO2Emission.HasValue)
            .GroupBy(r => (
                Time: new DateTime(
                    r.Minutes5DK.Year,
                    r.Minutes5DK.Month,
                    r.Minutes5DK.Day,
                    r.Minutes5DK.Hour,
                    (r.Minutes5DK.Minute / 15) * 15,
                    0,
                    r.Minutes5DK.Kind),
                PriceArea: r.PriceArea))
            .ToDictionary(
                g => g.Key,
                g => (decimal)g.Average(r => r.CO2Emission!.Value));

        if (slots.Count == 0)
        {
            _logger.LogInformation(
                "CO2  for {Date} is not published yet.",
                targetDate);

            return;
        }

        var filled = 0;

        foreach (var row in emptyRows)
        {
            if (slots.TryGetValue((row.Time, row.PriceArea), out var grams))
            {
                row.Co2EmissionGramsPerKwh = grams;
                filled++;
            }
        }

        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "CO2  for {Date}: {Filled} of {Empty} empty slots filled.",
            targetDate,
            filled,
            emptyRows.Count);
    }

    private async Task<List<Co2ImportResponse>> DownloadAsync(DateTime targetDate)
    {
        var filter = "{\"PriceArea\":[\"DK1\",\"DK2\"]}";

        var url =
            $"{ApiUrl}" +
            $"?start={targetDate:yyyy-MM-dd}" +
            $"&end={targetDate.AddDays(1):yyyy-MM-dd}" +
            "&columns=Minutes5UTC,Minutes5DK,PriceArea,CO2Emission" +
            "&limit=10000" +
            $"&filter={Uri.EscapeDataString(filter)}";

        var json = await _httpClient.GetFromJsonAsync<JsonElement>(url);

        // A missing "records" property is a real failure. An empty list only means "not published yet".
        if (!json.TryGetProperty("records", out var recordsElement))
        {
            throw new InvalidOperationException(
                "CO2EmisProg reply had no 'records' property.");
        }

        return recordsElement.Deserialize<List<Co2ImportResponse>>() ?? [];
    }
    #endregion
}