using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Hangfire;
using Hangfire.PostgreSql;
using SmartHomeApplicationAPI.Hangfire;
using SmartHomeApplicationAPI.Hangfire.Repository;
using SmartHomeApplicationAPI.Hangfire.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var secretsPath = Environment.GetEnvironmentVariable("SECRETS_PATH")
        ?? @"C:\Secrets\SmartHome\secrets.json";
    builder.Configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: true);
}

var smartHomeConn = builder.Configuration.GetConnectionString("SmartHome");
if (string.IsNullOrWhiteSpace(smartHomeConn))
    throw new InvalidOperationException("ConnectionStrings:SmartHome is missing");

var hangfireConnectionBuilder =
    new NpgsqlConnectionStringBuilder(smartHomeConn)
    {
        ApplicationName = "SmartHome-Hangfire"
    };

var hangfireConn = hangfireConnectionBuilder.ConnectionString;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
        policy.WithOrigins("https://localhost:7100")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddDbContext<SmartHomeDbContext>(options =>
{
    options.UseNpgsql(smartHomeConn);
});

builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    config.UsePostgreSqlStorage(
        o => o.UseNpgsqlConnection(hangfireConn),
        new PostgreSqlStorageOptions
        {    PrepareSchemaIfNecessary = true    });
});

//builder.Services.AddHangfireServer();

builder.Services.AddScoped<IElectricityPriceImportRepository, ElectricityPriceImportRepository>();
builder.Services.AddHttpClient<IElectricityPriceImportService, ElectricityPriceImportService>();

builder.Services.AddScoped<IElectricityPriceService, ElectricityPriceService>();
builder.Services.AddScoped<IElectricityPriceRepository, ElectricityPriceRepository>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowBlazor");

app.UseHangfireDashboard("/hangfire");

Jobs.Register();

app.MapControllers();

app.Run();