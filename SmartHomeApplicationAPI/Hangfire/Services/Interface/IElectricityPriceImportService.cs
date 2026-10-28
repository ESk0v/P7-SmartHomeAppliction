using Hangfire;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public interface IElectricityPriceImportService
{
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    Task StartImportAsync();
}