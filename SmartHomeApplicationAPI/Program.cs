using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure.Data;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;

var builder = WebApplication.CreateBuilder(args);

var smartHomeConn = builder.Configuration.GetConnectionString("SmartHome");

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