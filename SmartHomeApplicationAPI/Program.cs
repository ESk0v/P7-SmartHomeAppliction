using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure.Data;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;

var builder = WebApplication.CreateBuilder(args);

var secretsPath = Environment.GetEnvironmentVariable("SECRETS_PATH")
                  ?? @"C:\Secrets\SmartHome\secrets.json";
builder.Configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: true);

var smartHomeConn = builder.Configuration.GetConnectionString("SmartHome");
if (string.IsNullOrWhiteSpace(smartHomeConn))
    throw new InvalidOperationException("ConnectionStrings:SmartHome is missing");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
        policy.WithOrigins("https://localhost:7100")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseNpgsql(smartHomeConn));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowBlazor");

app.MapControllers();

app.Run();