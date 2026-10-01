using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using SmartHomeApplicationAPI.Infrastructure;

namespace SmartHomeApplicationAPI.Controller;

// TODO: TEMPORARY SERVER DIAGNOSTICS. Remove this controller after the deployment issue is resolved.
[ApiController]
[Route("api/temporary-diagnostics")]
public class TemporaryDiagnosticsController : ControllerBase
{
    private readonly SmartHomeDbContext _db;
    private readonly ILogger<TemporaryDiagnosticsController> _logger;

    public TemporaryDiagnosticsController(
        SmartHomeDbContext db,
        ILogger<TemporaryDiagnosticsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<TemporaryDiagnosticsResult>> Get(
        CancellationToken cancellationToken)
    {
        var result = new TemporaryDiagnosticsResult
        {
            CheckedAtUtc = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable(
                "ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        try
        {
            result.DatabaseCanConnect = await _db.Database.CanConnectAsync(
                cancellationToken);
        }
        catch (Exception exception)
        {
            result.DatabaseError = Describe(exception);
            _logger.LogError(exception, "Temporary database diagnostics failed.");
        }

        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();

            result.HangfireCanConnect = true;
            result.HangfireRecurringJobCount = recurringJobs.Count;
            result.HangfireRecurringJobIds = recurringJobs
                .Select(job => job.Id)
                .ToList();
        }
        catch (Exception exception)
        {
            result.HangfireError = Describe(exception);
            _logger.LogError(exception, "Temporary Hangfire diagnostics failed.");
        }

        result.IsHealthy =
            result.DatabaseCanConnect == true &&
            result.HangfireCanConnect == true;

        return Ok(result);
    }

    private static string Describe(Exception exception)
    {
        var messages = new List<string>();

        for (var current = exception;
             current is not null;
             current = current.InnerException)
        {
            messages.Add($"{current.GetType().Name}: {current.Message}");
        }

        return string.Join(" --> ", messages);
    }
}

// TODO: TEMPORARY SERVER DIAGNOSTICS. Remove with the controller.
public sealed class TemporaryDiagnosticsResult
{
    public DateTime CheckedAtUtc { get; set; }

    public string Environment { get; set; } = string.Empty;

    public bool IsHealthy { get; set; }

    public bool? DatabaseCanConnect { get; set; }

    public string? DatabaseError { get; set; }

    public bool? HangfireCanConnect { get; set; }

    public int? HangfireRecurringJobCount { get; set; }

    public List<string> HangfireRecurringJobIds { get; set; } = [];

    public string? HangfireError { get; set; }
}