using Hangfire;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public interface IElectricityPriceUpdateService
{
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    Task UpdatePricesAsync();
}