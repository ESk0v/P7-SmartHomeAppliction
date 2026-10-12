using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartHomeApplicationAPI.Hangfire.Repository;
using SmartHomeApplicationAPI.Hangfire.Services;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;

var builder = WebApplication.CreateBuilder(args);

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
    throw new InvalidOperationException("ConnectionStrings:SmartHome is missing");
}

var hangfireConn = new NpgsqlConnectionStringBuilder(smartHomeConn)
{
    ApplicationName = "SmartHome-Hangfire"
}.ConnectionString;

var runHangfireServer =
    builder.Configuration.GetValue<bool?>("Hangfire:RunServer")
    ?? !builder.Environment.IsDevelopment();

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

    if (builder.Environment.IsDevelopment())
    {
        config.UseInMemoryStorage();
    }
    else
    {
        config.UsePostgreSqlStorage(
            o => o.UseNpgsqlConnection(hangfireConn),
            new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true });
    }
});

if (runHangfireServer)
{
    builder.Services.AddHangfireServer(o => o.WorkerCount = 2);
}

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
    IPlanService,
    PlanService>();

builder.Services.AddScoped<
    IElectricityPriceRepository,
    ElectricityPriceRepository>();

builder.Services.AddScoped<
    IPlanRepository,
    PlanRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowBlazor");

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

if (runHangfireServer)
{
    app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(async () =>
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                SmartHomeApplicationAPI.Hangfire.Jobs.Register();
                app.Logger.LogInformation("Hangfire recurring jobs registered.");
                return;
            }
            catch (Exception ex)
            {
                app.Logger.LogError(
                    ex,
                    "Hangfire job registration failed (attempt {Attempt}/5)",
                    attempt);

                await Task.Delay(TimeSpan.FromSeconds(10 * attempt));
            }
        }
    }));
}

app.Run();