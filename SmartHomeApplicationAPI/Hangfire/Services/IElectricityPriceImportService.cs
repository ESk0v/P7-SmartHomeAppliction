using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public interface IElectricityPriceImportService
{
    Task StartImportAsync();

    Task ImportAttemptAsync(
        DateTime targetDate,
        DateTime retryWindowEnds,
        int attempt);
}