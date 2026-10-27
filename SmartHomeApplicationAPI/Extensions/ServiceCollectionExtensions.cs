
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

        //REPOSITORIES
        services.AddScoped<IElectricityPriceRepository, ElectricityPriceRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IStatisticsRepository, StatisticsRepository>();

        //HANGFIRE
        services.AddHttpClient<IElectricityPriceImportService, ElectricityPriceImportService>();
        services.AddHttpClient<ICo2ImportService, Co2ImportService>();

        services.AddScoped<IElectricityPriceImportRepository, ElectricityPriceImportRepository>();
        services.AddScoped<ICo2ImportRepository, Co2ImportRepository>();


        return services;
    }
}
