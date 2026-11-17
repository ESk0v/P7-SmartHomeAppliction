using System.Net.Http.Json;
using SmartHomeApplicationAPI.DTOs.EnergiDataService;
using SmartHomeApplicationAPI.Hangfire.Repository;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public class ElectricityPriceUpdateService
{
    private const string DayAheadApiUrl =
        "https://api.energidataservice.dk/dataset/DayAheadPrices";

    private static readonly TimeZoneInfo CopenhagenTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private readonly HttpClient _httpClient;
    private readonly IElectricityPriceImportRepository _repository;

    public ElectricityPriceUpdateService(
        HttpClient httpClient,
        IElectricityPriceImportRepository repository)
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

    private async Task<bool> ImportDayAheadPricesAsync()
    {
        // 1. Determine tomorrow's date in Copenhagen time.
        var tomorrow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            CopenhagenTimeZone).Date.AddDays(1);

        return await ImportPricesForDateAsync(tomorrow);
    }

    #region DayAheadPrices Helpers
    private async Task<bool> ImportPricesForDateAsync(DateTime targetDate)
    {
        // 1. Check whether all actual prices already exist in the database.

        // 2. Fetch tomorrow's day-ahead prices from Energi Data Service.

        // 3. Validate that we received the complete dataset for DK1 and DK2.

        // 4. Replace existing forecast records with actual prices.

        // 5. Return true only when actual prices are successfully stored.

        return false; // Temporary until implemented.
    }
    #endregion

    private async Task ImportPricesForDateAsync(DateTime targetDate)
    {
        // We'll implement this method next.
    }
}