
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

        //REPOSITORIES
        services.AddScoped<IElectricityPriceRepository, ElectricityPriceRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IStatisticsRepository, StatisticsRepository>();
        services.AddScoped<ICreatePlanRepository, CreatePlanRepository>();


        //HANGFIRE
        services.AddHttpClient<IElectricityPriceImportService, ElectricityPriceImportService>();
        services.AddScoped<IElectricityPriceImportRepository, ElectricityPriceImportRepository>();

        return services;
    }
}
