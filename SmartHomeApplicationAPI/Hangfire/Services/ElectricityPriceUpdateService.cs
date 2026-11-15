using System.Net.Http.Json;
using System.Text.Json;
using SmartHomeApplicationAPI.DTOs.EnergiDataService;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.Hangfire.Repository;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public class ElectricityPriceUpdateService : IElectricityPriceUpdateService
{
    private const string DayAheadApiUrl =
        "https://api.energidataservice.dk/dataset/DayAheadPrices";

    private static readonly TimeZoneInfo CopenhagenTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private readonly HttpClient _httpClient;
    private readonly IElectricityPriceUpdateRepository _repository;

    public ElectricityPriceUpdateService(
        HttpClient httpClient,
        IElectricityPriceUpdateRepository repository)
    {
        _httpClient = httpClient;
        _repository = repository;
    }

    public async Task UpdatePricesAsync()
    {
        bool actualPricesReady = await ImportDayAheadPricesAsync();

        if (!actualPricesReady)
            return;

        await UpdateForecastPricesAsync();
    }

    #region DayAheadPrices
    private async Task<bool> ImportDayAheadPricesAsync()
    {
        var tomorrow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            CopenhagenTimeZone).Date.AddDays(1);

        // 1. Get existing prices for tomorrow.
        var existingPrices = await _repository.GetPricesAsync(
            tomorrow,
            tomorrow.AddDays(1));

        // 2. Check whether all actual prices already exist.
        if (existingPrices.Count == 192 && existingPrices.All(price => !price.IsPredicted))
        {
            return true;
        }

        // 3. Fetch tomorrow's actual prices.
        var actualPrices = await FetchDayAheadPricesAsync(tomorrow);

        // 4. Validate the complete dataset.
        if (actualPrices.Count(price => price.PriceArea == "DK1") != 96 ||
            actualPrices.Count(price => price.PriceArea == "DK2") != 96)
        {
            return false;
        }

        // 5. Match downloaded prices with existing database records.
        var existingByKey = existingPrices.ToDictionary(
            price => (price.Time, price.PriceArea));

        var newPrices = new List<Electricity>();

        foreach (var actualPrice in actualPrices)
        {
            var key = (actualPrice.Time, actualPrice.PriceArea);

            if (existingByKey.TryGetValue(key, out var existingPrice))
            {
                existingPrice.PricePerKwh = actualPrice.PricePerKwh;
                existingPrice.IsPredicted = false;
            }
            else
            {
                newPrices.Add(actualPrice);
            }
        }

        // 6. Save updated and newly inserted prices.
        await _repository.SavePricesAsync(newPrices);

        return true;
    }

    private async Task<List<Electricity>> FetchDayAheadPricesAsync(
        DateTime targetDate)
    {
        var filter = "{\"PriceArea\":[\"DK1\",\"DK2\"]}";

        var url =
            $"{DayAheadApiUrl}" +
            $"?start={targetDate:yyyy-MM-dd}" +
            $"&end={targetDate.AddDays(1):yyyy-MM-dd}" +
            "&columns=TimeUTC,TimeDK,PriceArea,DayAheadPriceDKK" +
            "&limit=10000" +
            $"&filter={Uri.EscapeDataString(filter)}";

        var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

        if (!response.TryGetProperty("records", out var records))
        {
            throw new InvalidOperationException(
                "DayAheadPrices response contains no 'records' property.");
        }

        var data = records.Deserialize<List<ElectricityImportResponse>>() ?? [];

        return data
            .Where(record => record.DayAheadPriceDkk.HasValue)
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

    #region ForecastPrices
    private async Task UpdateForecastPricesAsync()
    {
        // 1. Determine the day after tomorrow.
        var forecastDate = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            CopenhagenTimeZone).Date.AddDays(2);

        // 2. Get existing prices for that day.
        var existingPrices = await _repository.GetPricesAsync(
            forecastDate,
            forecastDate.AddDays(1));

        // 3. Check whether all forecasts already exist.
        if (existingPrices.Count == 192 &&
            existingPrices.Count(p =>
                p.PriceArea == "DK1" && p.IsPredicted) == 96 &&
            existingPrices.Count(p =>
                p.PriceArea == "DK2" && p.IsPredicted) == 96 &&
            existingPrices.Select(p => (p.Time, p.PriceArea))
                .Distinct().Count() == 192)
        {
            return;
        }

        // 4. Fetch forecasts for both price areas.
        var dk1Prices = await FetchForecastPricesAsync("DK1", forecastDate);
        var dk2Prices = await FetchForecastPricesAsync("DK2", forecastDate);

        // 5. Validate that both areas have 96 unique timestamps.
        if (dk1Prices.Count != 96 ||
            dk2Prices.Count != 96 ||
            dk1Prices.Select(p => p.Time).Distinct().Count() != 96 ||
            dk2Prices.Select(p => p.Time).Distinct().Count() != 96)
        {
            return;
        }

        // 6. Combine the forecasts.
        var forecastPrices = dk1Prices.Concat(dk2Prices).ToList();

        // 7. Identify existing records by timestamp and price area.
        var existingKeys = existingPrices
            .Select(p => (p.Time, p.PriceArea))
            .ToHashSet();

        // 8. Insert only prices that do not already exist.
        var newPrices = forecastPrices
            .Where(price => !existingKeys.Contains(
                (price.Time, price.PriceArea)))
            .ToList();

        // 9. Save the missing forecasts.
        if (newPrices.Count > 0)
        {
            await _repository.SavePricesAsync(newPrices);
        }
    }

    private async Task<List<Electricity>> FetchForecastPricesAsync(
        string priceArea,
        DateTime targetDate)
    {
        var url =
            $"https://elpriser.org/api/forecast" +
            $"?area={priceArea}&mode=spot_inkl";

        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var predictions = new List<Electricity>();

        if (!document.RootElement.TryGetProperty("days", out var days) ||
            days.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "Forecast response contains no 'days' array.");
        }

        foreach (var day in days.EnumerateArray())
        {
            var date = DateTime.Parse(
                day.GetProperty("date").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);

            if (date.Date != targetDate.Date)
                continue;

            if (!day.TryGetProperty("prices", out var prices) ||
                prices.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var hourPrice in prices.EnumerateArray())
            {
                var hour = hourPrice.GetProperty("hour").GetInt32();
                var price = hourPrice.GetProperty("price").GetDecimal();

                var hourStart = date.Date.AddHours(hour);

                // Convert each hourly price into four 15-minute prices.
                for (int minute = 0; minute < 60; minute += 15)
                {
                    predictions.Add(new Electricity
                    {
                        Time = hourStart.AddMinutes(minute),
                        PriceArea = priceArea,
                        PricePerKwh = price,
                        IsPredicted = true
                    });
                }
            }
        }

        return predictions.OrderBy(price => price.Time).ToList();
    }

    #endregion
}