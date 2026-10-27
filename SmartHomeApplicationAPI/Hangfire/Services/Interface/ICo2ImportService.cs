using Hangfire;

namespace SmartHomeApplicationAPI.Hangfire.Services;

public interface ICo2ImportService
{
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    Task StartImportAsync();
}