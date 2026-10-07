using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
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

// User login, auth and authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "auth_smartHome";
        options.Cookie.SameSite = SameSiteMode.None; // something with localhost cross-port
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.AccessDeniedPath = "/access-denied";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
        policy.WithOrigins("https://localhost:7100")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
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
    IElectricityPriceRepository,
    ElectricityPriceRepository>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowBlazor");

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
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