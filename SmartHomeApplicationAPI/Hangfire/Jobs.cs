// Job that calls the Energi Data Service API for the "DayAheadPrices" dataset and stores the data in the database. We use Hangfire to schedule this job to run every day at 13.00 (run every five minutes until 14.00 same day to ensure the data is up-to-date) to ensure that the data for the previous day is available.
// Here, jobs should only be declared, while the actual execution should be done in a service file in "Hangfire/Services" folder. We might include repositories and models to maintain the pattern used elsewhere.
using Hangfire;
using SmartHomeApplicationAPI.Hangfire.Services;

namespace SmartHomeApplicationAPI.Hangfire;

public static class Jobs
{
    public const string ElectricityPriceImport =
        "electricity-price-import";

    public static void Register()
    {
        RecurringJob.AddOrUpdate<IElectricityPriceImportService>(
            ElectricityPriceImport,
            service => service.StartImportAsync(),
            Cron.Daily(15, 30),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    "Europe/Copenhagen")
            });
    }
}
