using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using System.Data.Common;

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

            if (result.DatabaseCanConnect == true)
            {
                await ReadDatabaseDetailsAsync(result, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            result.DatabaseDetailsError = Describe(exception);
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
            result.HangfireCanConnect = false;
            result.HangfireError = Describe(exception);
            _logger.LogError(exception, "Temporary Hangfire diagnostics failed.");
        }

        result.IsHealthy =
            result.DatabaseCanConnect == true &&
            result.HangfireCanConnect == true;

        return Ok(result);
    }

    private async Task ReadDatabaseDetailsAsync(
        TemporaryDiagnosticsResult result,
        CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();

        await connection.OpenAsync(cancellationToken);

        result.DatabaseUser = await ExecuteScalarAsync(
            connection,
            "SELECT current_user");

        result.DatabaseName = await ExecuteScalarAsync(
            connection,
            "SELECT current_database()");

        result.DatabaseServer = await ExecuteScalarAsync(
            connection,
            """
            SELECT COALESCE(inet_server_addr()::text, 'local')
                   || ':' ||
                   COALESCE(inet_server_port()::text, 'unknown')
            """);

        result.HangfireSchemaExists = await ExecuteBooleanAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.schemata
                WHERE schema_name = 'hangfire'
            )
            """);

        result.HangfireJobTableExists = await ExecuteBooleanAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'hangfire'
                  AND table_name = 'job'
            )
            """);

        result.HangfireLockTableExists = await ExecuteBooleanAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'hangfire'
                  AND table_name = 'lock'
            )
            """);

        result.HangfireJobSelectAllowed = await ExecuteBooleanAsync(
            connection,
            """
            SELECT has_table_privilege(
                current_user,
                'hangfire.job',
                'SELECT'
            )
            """);

        await connection.CloseAsync();
    }

    private static async Task<string?> ExecuteScalarAsync(
        DbConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var value = await command.ExecuteScalarAsync();

        return value?.ToString();
    }

    private static async Task<bool> ExecuteBooleanAsync(
        DbConnection connection,
        string sql)
    {
        var value = await ExecuteScalarAsync(connection, sql);

        return bool.TryParse(value, out var result) && result;
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

    public string? DatabaseUser { get; set; }

    public string? DatabaseName { get; set; }

    public string? DatabaseServer { get; set; }

    public string? DatabaseDetailsError { get; set; }

    public bool? HangfireSchemaExists { get; set; }

    public bool? HangfireJobTableExists { get; set; }

    public bool? HangfireLockTableExists { get; set; }

    public bool? HangfireJobSelectAllowed { get; set; }

    public bool? HangfireCanConnect { get; set; }

    public int? HangfireRecurringJobCount { get; set; }

    public List<string> HangfireRecurringJobIds { get; set; } = [];

    public string? HangfireError { get; set; }
}