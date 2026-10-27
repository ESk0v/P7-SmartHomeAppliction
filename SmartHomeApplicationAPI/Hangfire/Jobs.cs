using Hangfire;
using SmartHomeApplicationAPI.Hangfire.Services;

namespace SmartHomeApplicationAPI.Hangfire;

public static class Jobs
{
    public const string ElectricityPriceImport =
        "electricity-price-import";

    private static readonly TimeZoneInfo Copenhagen =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    public static void Register()
    {
        RecurringJob.AddOrUpdate<IElectricityPriceImportService>(
            ElectricityPriceImport,
            service => service.StartImportAsync(),
            "*/5 13 * * *",
            new RecurringJobOptions
            {
                TimeZone = Copenhagen
            });
        
//        RecurringJob.AddOrUpdate<ICo2ImportService>(
//            Co2ForecastImport,
//            service => service.ImportForecastAsync(),
//           "*/15 15-17 * * *",
//            options);

//        RecurringJob.AddOrUpdate<ICo2ImportService>(
//            Co2ActualsImport,
//            service => service.ImportActualsAsync(),
//            "5 * * * *",
//            options);
        
    }
}
