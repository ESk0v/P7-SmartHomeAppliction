using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.Tests.Infrastructure;
using Utilities.Models;
using SmartHomeApplicationAPI.Hangfire.Services;

namespace SmartHomeApplicationAPI.Tests.Integration;

public class ForecastingIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private static readonly DateTime ForecastDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2), DateTimeKind.Unspecified);

    public ForecastingIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // Helper to remove predicted == true
    private async Task CleanPredictionsAsync(SmartHomeDbContext dbContext)
    {
        var testDate = ForecastDate;
        await dbContext.Electricity
            .Where(e => e.IsPredicted || e.Time >= testDate)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Post_ForecastedPrices_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
        await CleanPredictionsAsync(dbContext);
        
        // Act
        var forecastingService = scope.ServiceProvider.GetRequiredService<IForecastingService>();
        var forecast = await forecastingService.GenerateForecastForAreaAsync("DK1", 4);

        var targetDate = ForecastDate;
        var startTime = targetDate.AddHours(10);
        var endTime = targetDate.AddHours(10).AddMinutes(45);
        var savedPrices = await dbContext.Electricity
            .Where(e => e.IsPredicted && e.Time <= endTime && e.Time >= startTime)
            .ToListAsync();

        // Assert
        Assert.NotNull(forecast);
        Assert.Equal("DK1", forecast!.PriceArea);
        Assert.Equal(4, forecast.Predictions.Count);
        Assert.Equal(4, savedPrices.Count);
    }

    [Fact]
    public async Task EmptyForecast_ReturnsNoPredictions_AndPreservesExistingPredictions()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
        await CleanPredictionsAsync(dbContext);

        dbContext.Electricity.Add(new Electricity
        {
           Time = ForecastDate.AddHours(10),
           PriceArea = "EMP",
           PricePerKwh = 1.25m,
           IsPredicted = true 
        });
        await dbContext.SaveChangesAsync();

        // Act
        var forecastingService = scope.ServiceProvider.GetRequiredService<IForecastingService>();
        var forecast = await forecastingService.GenerateForecastForAreaAsync("EMP", 4);

        // Assert
        var remainingPredictions = await dbContext.Electricity
            .Where(e => e.PriceArea == "EMP" && e.IsPredicted)
            .ToListAsync();
        
        Assert.Empty(forecast.Predictions);
        Assert.Single(remainingPredictions);
        Assert.Equal(ForecastDate.AddHours(10), remainingPredictions[0].Time);
    }
}



