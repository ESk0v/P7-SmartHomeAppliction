using Controller.Dashboard;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Service;

public class OptimizationValidationTests
{
    [Fact]
    public async Task Optimize_WhenPresetPowerExceedsFuseLimit_RejectsItBeforeContactingSolver()
    {
        var repo = new Mock<ICreatePlanRepository>();
        repo.Setup(value => value.GetElectricityPricesAsync("DK1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Repeat(1d, 8).ToList());
        var device = new PlanUserDeviceDto(1, 1, "Dishwasher", "", "", "Non-Interruptible", "Dishwasher", null);
        var preset = new DevicePreset { Id = 9, DeviceId = 1, ConsumptionInKwh = 10m, DurationInMinutes = 15 };
        repo.Setup(value => value.GetDevicesWithPresetsAsync(1))
            .ReturnsAsync(new List<(PlanUserDeviceDto, DevicePreset)> { (device, preset) });
        repo.Setup(value => value.GetActivePlansAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Plan>());
        var service = new CreatePlanService(repo.Object, NullLogger<CreatePlanService>.Instance);

        var error = await Assert.ThrowsAsync<ValidationException>(() => service.OptimizeAsync(new OptimizationRequestDTO
        {
            Area = "DK1", UserDeviceId = 1, DevicePresetId = 9,
            StartWindow = DateTime.UtcNow, EndWindow = DateTime.UtcNow.AddHours(2)
        }));

        Assert.Contains("40 kW", error.Message);
        Assert.Contains("Choose another preset", error.Message);
    }

    [Fact]
    public async Task Optimize_WhenServiceReportsAnInfeasiblePlan_ReturnsReadable422Response()
    {
        var request = new OptimizationRequestDTO();
        var service = new Mock<ICreatePlanService>();
        service.Setup(value => value.OptimizeAsync(request)).ThrowsAsync(new ValidationException("Choose a longer time window."));
        var controller = new CreatePlanController(service.Object, NullLogger<CreatePlanController>.Instance);

        var result = await controller.Optimize(request);

        var response = Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        Assert.Equal("Choose a longer time window.", response.Value);
    }
}
