using System.Net;
using System.Reflection;
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
    public async Task Optimize_WhenSolverIsInfeasible_ReturnsGuidanceWithoutAssumingTheCause()
    {
        var repo = new Mock<ICreatePlanRepository>();
        repo.Setup(value => value.GetElectricityPricesAsync("DK1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Repeat(1d, 8).ToList());
        repo.Setup(value => value.GetDevicesWithPresetsAsync(1)).ReturnsAsync(new List<(PlanUserDeviceDto, DevicePreset)>
        {
            (new(1, 1, "Car", "", "", "Interruptible", "Car", 80m),
                new() { Id = 9, BatteryCapacityInKwh = 80m, ConsumptionInKwh = 11m })
        });
        repo.Setup(value => value.GetActivePlansAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Plan>());
        using var handler = new InfeasibleSolverHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://solver.test") };
        var service = new CreatePlanService(repo.Object, NullLogger<CreatePlanService>.Instance);
        typeof(CreatePlanService).GetField("_http", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(service, client);

        var error = await Assert.ThrowsAsync<ValidationException>(() => service.OptimizeAsync(new OptimizationRequestDTO
        {
            Area = "DK1", UserDeviceId = 1, DevicePresetId = 9, ChargeStopPercentage = 80,
            StartWindow = DateTime.UtcNow, EndWindow = DateTime.UtcNow.AddHours(2)
        }));

        Assert.Contains("selected time window and power limits", error.Message);
        Assert.Contains("lower energy demand", error.Message);
        Assert.DoesNotContain("would exceed the fuse limit", error.Message);
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

    private sealed class InfeasibleSolverHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent("{\"detail\":\"Solver status: Infeasible\"}")
            });
    }
}
