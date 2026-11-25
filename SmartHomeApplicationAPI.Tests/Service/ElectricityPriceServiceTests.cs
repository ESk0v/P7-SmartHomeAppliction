using Moq;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Service;

public class ElectricityPriceServiceTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public async Task GetPricesForDateAsync_PreservesPredictionFlagWhenAveragingHours(
        bool firstPredicted, bool secondPredicted, bool expectedPredicted)
    {
        var hour = new DateTime(2026, 11, 17, 12, 0, 0);
        var repository = new Mock<IElectricityPriceRepository>();
        repository.Setup(r => r.GetPricesForDateAsync("DK1", It.IsAny<DateTime>()))
            .ReturnsAsync(new List<HourPriceDto>
            {
                new(hour.AddHours(1), 5m),
                new(hour.AddMinutes(30), 4m, secondPredicted),
                new(hour, 2m, firstPredicted)
            });
        var service = new ElectricityPriceService(repository.Object);

        var prices = await service.GetPricesForDateAsync("DK1");

        Assert.Equal(2, prices.Count);
        Assert.Equal(new HourPriceDto(hour, 3m, expectedPredicted), prices[0]);
        Assert.Equal(new HourPriceDto(hour.AddHours(1), 5m, false), prices[1]);
    }

    [Fact]
    public async Task GetPricesForDateAsync_AveragesAvailableCo2_AndPreservesMissingHoursAndZero()
    {
        var hour = new DateTime(2026, 11, 20, 12, 0, 0);
        var repository = new Mock<IElectricityPriceRepository>();
        repository.Setup(r => r.GetPricesForDateAsync("DK2", It.IsAny<DateTime>()))
            .ReturnsAsync(new List<HourPriceDto>
            {
                new(hour, 2m, false, 100m),
                new(hour.AddMinutes(15), 4m, false, 200m),
                new(hour.AddMinutes(30), 3m),
                new(hour.AddHours(1), 5m, true),
                new(hour.AddHours(2), 1m, false, 0m)
            });

        var result = await new ElectricityPriceService(repository.Object).GetPricesForDateAsync("DK2");

        Assert.Equal(new[] { hour, hour.AddHours(1), hour.AddHours(2) }, result.Select(point => point.Hour));
        Assert.Equal(new decimal?[] { 150m, null, 0m }, result.Select(point => point.Co2EmissionGramsPerKwh));
        Assert.Equal(new[] { 3m, 5m, 1m }, result.Select(point => point.Price));
        Assert.True(result[1].IsPredicted);
    }
}
