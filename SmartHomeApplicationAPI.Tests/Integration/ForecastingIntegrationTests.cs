using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.Tests.Infrastructure;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Integration;

public class ForecastingIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ForecastingIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // Helper to remove predicted == true
    private async Task CleanPredictionsAsync(SmartHomeDbContext dbContext)
    {
        var testDate = new DateTime(2026, 11, 12, 0, 0 ,0);
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
        var response = await _client.PostAsync(
            "/api/forecast?priceArea=dk1&stepsAhead=4",
            content: null);

        var forecast = await response.Content.ReadFromJsonAsync<ForecastingResponseDto>();

        var targetDate = new DateTime(2026, 11, 12, 0, 0, 0);
        var startTime = targetDate.AddHours(10);
        var endTime = targetDate.AddHours(10).AddMinutes(45);
        var savedPrices = await dbContext.Electricity
            .Where(e => e.IsPredicted && e.Time <= endTime && e.Time >= startTime)
            .ToListAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(forecast);
        Assert.Equal("DK1", forecast!.PriceArea);
        Assert.Equal(4, forecast.Predictions.Count);
        Assert.Equal(4, savedPrices.Count);
    }


    [Fact]
    public async Task Post_ForecastedPrices_VerifyPredictionsAtOrBeforeNotTimeExcluded()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
        await CleanPredictionsAsync(dbContext);

        dbContext.Electricity.Add(new Electricity
        {
           Time = new DateTime(2026, 11, 12, 10, 15, 0),
           PriceArea = "DK1",
           PricePerKwh = 2.00m,
           IsPredicted = false 
        });
        await dbContext.SaveChangesAsync();

        var initialCount = await dbContext.Electricity.CountAsync();

        // Act
        var response = await _client.PostAsync(
            "/api/forecast?priceArea=dk1&stepsAhead=4",
            content: null); 
        
        var forecast = await response.Content.ReadFromJsonAsync<ForecastingResponseDto>();
        var predictedTrueFromDB = await dbContext.Electricity
            .Where(e => e.IsPredicted == true)
            .ToListAsync();
         
        var totalRows = await dbContext.Electricity
            .ToListAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(forecast);
        Assert.Equal(2, forecast.Predictions.Count);
        Assert.Equal(2, predictedTrueFromDB.Count);
        Assert.Equal(initialCount + 2, totalRows.Count);
        // Should exclude 10:15 since IsPredicted is false here (Seeded)
        Assert.DoesNotContain(predictedTrueFromDB, price => price.Time <= new DateTime(2026, 11, 12, 10, 15, 0));
    }
}


// NOTES FOR MYSELF
// Test one Should: 
// Create HttpClient from factory
// Send: POST /api/forecast?priceArea=dk1&stepsAhead=4
// Assert resp is 200 OK
// Assert area is DK1
// Assert that four predictions are returned
// open a database scope and query electricty
// Assert that four predicted rows were saved with the price 1.25

// Test two should:
// Seed postgres qwith non-predcted price:
// then run forecast and verify that the predictions at or before that time are exlcluded
