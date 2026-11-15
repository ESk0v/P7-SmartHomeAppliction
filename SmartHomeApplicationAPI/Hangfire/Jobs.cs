using Hangfire;
using SmartHomeApplicationAPI.Hangfire.Services;

namespace SmartHomeApplicationAPI.Hangfire;

public static class Jobs
{
    public const string ElectricityPriceUpdate = "electricity-price-import";
    public const string Co2Import = "co2-import";

    private static readonly TimeZoneInfo Copenhagen =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    public static void Register()
    {
        var options = new RecurringJobOptions
        {
            TimeZone = Copenhagen
        };

        // Electricity prices and forecasting: every 5 minutes from 13:00 to 13:55
        RecurringJob.AddOrUpdate<IElectricityPriceUpdateService>(
            ElectricityPriceUpdate,
            service => service.UpdatePricesAsync(),
            "*/5 13 * * *",
            options);

        // CO2: unchanged
        RecurringJob.AddOrUpdate<ICo2ImportService>(
            Co2Import,
            service => service.StartImportAsync(),
            "*/15 15-19 * * *",
            options);
    }
}