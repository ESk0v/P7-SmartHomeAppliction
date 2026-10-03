using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using SmartHomeApplicationAPI.Hangfire.Repository;
using SmartHomeApplicationAPI.Hangfire.Services;
using Npgsql;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Configuration / Secrets
// --------------------------------------------------

if (!builder.Environment.IsDevelopment())
{
    var secretsPath = Environment.GetEnvironmentVariable("SECRETS_PATH")
        ?? @"C:\Secrets\SmartHome\secrets.json";

    builder.Configuration.AddJsonFile(
        secretsPath,
        optional: false,
        reloadOnChange: true);
}

var smartHomeConn = builder.Configuration.GetConnectionString("SmartHome");

if (string.IsNullOrWhiteSpace(smartHomeConn))
{
    throw new InvalidOperationException(
        "ConnectionStrings:SmartHome is missing");
}

// --------------------------------------------------
// Hangfire connection
// --------------------------------------------------

var hangfireConnectionBuilder =
    new NpgsqlConnectionStringBuilder(smartHomeConn)
    {
        ApplicationName = "SmartHome-Hangfire"
    };

var hangfireConn = hangfireConnectionBuilder.ConnectionString;

// --------------------------------------------------
// CORS
// --------------------------------------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
        policy.WithOrigins("https://localhost:7100")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// --------------------------------------------------
// Entity Framework / PostgreSQL
// --------------------------------------------------

builder.Services.AddDbContext<SmartHomeDbContext>(options =>
{
    options.UseNpgsql(smartHomeConn);
});

// --------------------------------------------------
// Hangfire
// --------------------------------------------------
// IMPORTANT:
// Storage is enabled for this test.
// Hangfire Server is NOT enabled yet.
// Jobs.Register() is NOT called yet.
// Dashboard is NOT enabled yet.
// --------------------------------------------------

builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    config.UsePostgreSqlStorage(
        o => o.UseNpgsqlConnection(hangfireConn),
        new PostgreSqlStorageOptions
        {
            PrepareSchemaIfNecessary = true
        });
});

// --------------------------------------------------
// Application services
// --------------------------------------------------

builder.Services.AddScoped<
    IElectricityPriceImportRepository,
    ElectricityPriceImportRepository>();

builder.Services.AddHttpClient<
    IElectricityPriceImportService,
    ElectricityPriceImportService>();

builder.Services.AddScoped<
    IElectricityPriceService,
    ElectricityPriceService>();

builder.Services.AddScoped<
    IElectricityPriceRepository,
    ElectricityPriceRepository>();

// --------------------------------------------------
// Controllers
// --------------------------------------------------

builder.Services.AddControllers();

// --------------------------------------------------
// Build application
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// Middleware
// --------------------------------------------------

app.UseCors("AllowBlazor");

// --------------------------------------------------
// Hangfire intentionally NOT enabled yet
// --------------------------------------------------

// app.UseHangfireDashboard("/hangfire");
// app.MapHangfireDashboard("/hangfire");

// builder.Services.AddHangfireServer();
// Jobs.Register();

// --------------------------------------------------
// API
// --------------------------------------------------

app.MapControllers();

app.Run();