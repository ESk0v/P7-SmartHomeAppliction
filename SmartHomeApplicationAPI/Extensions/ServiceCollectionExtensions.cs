
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using SmartHomeApplicationAPI.Hangfire.Services;
using SmartHomeApplicationAPI.Hangfire.Repository;

namespace SmartHomeApplicationAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        //SERVICES
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IElectricityPriceService, ElectricityPriceService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<ICreatePlanService, CreatePlanService>();
        services.AddScoped<IOptimizationService, OptimizationService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddHttpClient<IForecastingService, ForecastingService>((sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var pythonUrl = config["ForecastingApiUrl"] ?? "http://127.0.0.1:6767";
            client.BaseAddress = new Uri(pythonUrl);
        });

        //REPOSITORIES
        services.AddScoped<IElectricityPriceRepository, ElectricityPriceRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IStatisticsRepository, StatisticsRepository>();
        services.AddScoped<ICreatePlanRepository, CreatePlanRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IHistoryRepository, HistoryRepository>();
        services.AddScoped<IForecastingRepository, ForecastingRepository>();


        //HANGFIRE
        services.AddHttpClient<IElectricityPriceImportService, ElectricityPriceImportService>();
        services.AddHttpClient<ICo2ImportService, Co2ImportService>();

        services.AddScoped<IElectricityPriceImportRepository, ElectricityPriceImportRepository>();
        services.AddScoped<ICo2ImportRepository, Co2ImportRepository>();


        return services;
    }
}
