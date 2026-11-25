using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartHomeApplicationAPI.DTOs.Optimization;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Service;

public class OptimizationDeviceSelectionTests
{
    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(2, 4, 2)]
    public async Task Optimize_ReturnsTheRequestedEvsSchedule_EvenWhenNicknamesMatch(int deviceId, int startStep, decimal cost)
    {
        var repository = new Mock<ICreatePlanRepository>();
        var preset = new DevicePreset { Id = 9, BatteryCapacityInKwh = 80m, ConsumptionInKwh = 11m };
        var rows = Enumerable.Range(1, 2).Select(id => (
            new PlanUserDeviceDto(id, 1, "Same nickname", "", "", "Interruptible", "Car", 80m), preset)).ToList();
        repository.Setup(repo => repo.GetDevicesWithPresetsAsync(deviceId)).ReturnsAsync(rows);
        repository.Setup(repo => repo.GetElectricityPricesAsync("DK1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Repeat(1d, 8).ToList());
        repository.Setup(repo => repo.GetCo2EmissionsAsync("DK1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Repeat(200d, 8).ToList());
        repository.Setup(repo => repo.GetActivePlansAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Range(1, 2).Select(id => new Plan
            {
                UserDeviceId = id, DevicePresetId = 9,
                WindowStartTime = DateTime.UtcNow, WindowEndTime = DateTime.UtcNow.AddHours(2)
            }).ToList());
        DateTime solverStart = default;
        using var handler = new SolverHandler(async request =>
        {
            var solver = (await request.Content!.ReadFromJsonAsync<SolverReqDTO>())!;
            solverStart = solver.StartTime;
            Assert.Equal(2, solver.Loads.Count);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    appliances = Array.Empty<object>(),
                    evs = new[]
                    {
                        new { name = "Same nickname", power_kw = new[] { 4d, 0, 0, 0, 0, 0, 0, 0 } },
                        new { name = "Same nickname", power_kw = new[] { 0d, 0, 0, 0, 4, 4, 0, 0 } }
                    }
                })
            };
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://solver.test") };
        var service = new CreatePlanService(repository.Object, NullLogger<CreatePlanService>.Instance);
        typeof(CreatePlanService).GetField("_http", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(service, client);

        var result = await service.OptimizeAsync(new OptimizationRequestDTO
        {
            Area = "DK1",
            UserDeviceId = deviceId,
            DevicePresetId = 9,
            ChargeStopPercentage = 80,
            Priority = "Cheapest",
            StartWindow = DateTime.UtcNow.AddHours(deviceId == 2 ? 1 : 0),
            EndWindow = DateTime.UtcNow.AddHours(2)
        });

        var interval = Assert.Single(result);

var copenhagenTimeZone =
    TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

var expectedStart = TimeZoneInfo.ConvertTimeToUtc(
    solverStart.AddMinutes(15 * startStep),
    copenhagenTimeZone);

Assert.Equal(expectedStart, interval.StartTime);
Assert.Equal(DateTimeKind.Utc, interval.StartTime.Kind);
Assert.Equal(cost, interval.EstimatedPrice);
    }

    private sealed class SolverHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
