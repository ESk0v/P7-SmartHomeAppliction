using Hangfire;
using SmartHomeApplicationAPI.Hangfire.Services;

namespace SmartHomeApplicationAPI.Hangfire;

public static class Jobs
{
    public const string ElectricityPriceImport = "electricity-price-import";
    public const string Co2Import = "co2-import";
    public const string DailyPriceForecast = "daily-price-forecast";

    private static readonly TimeZoneInfo Copenhagen =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    public static void Register()
    {
        var options = new RecurringJobOptions
        {
            TimeZone = Copenhagen
        };

        // Electricity prices: every 5 minutes from 13:00 to 13:55
        RecurringJob.AddOrUpdate<IElectricityPriceImportService>(
            ElectricityPriceImport,
            service => service.StartImportAsync(),
            "*/5 13 * * *",
            options);

        // CO2: every 15 minutes from 15:00 to 19:45
        RecurringJob.AddOrUpdate<ICo2ImportService>(
            Co2Import,
            service => service.StartImportAsync(),
            "*/15 15-19 * * *",
            options);

        // Forecast: every day at 14:00 Copenhagen time
        RecurringJob.AddOrUpdate<IForecastingService>(
            DailyPriceForecast,
            service => service.RunDailyForecastJobAsync(),
            "0 14 * * *",
            options);
    }
}