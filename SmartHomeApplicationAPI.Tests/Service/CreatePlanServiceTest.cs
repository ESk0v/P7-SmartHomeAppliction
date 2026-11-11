using Moq;
using Xunit;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Service;

public class CreatePlanServiceTests
{
    [Fact]
    public async Task GetPresetsAsync_WithValidDeviceId_ReturnsExpectedPresets()
    {
        // Arrange
        var mockRepo = new Mock<ICreatePlanRepository>();
        int deviceId = 34;

        var expectedPresets = new List<PresetDto>
        {
            new PresetDto
            {
                id = 33,
                presetName = "standard",
                consumption = 20.000m,
                duration = 180,
                batteryCapacityInKwh = 400.000m
            }
        };

        mockRepo.Setup(repo => repo.GetPresetsAsync(deviceId))
            .Returns(Task.FromResult(expectedPresets));

        var service = new CreatePlanService(mockRepo.Object);

        // Act
        var result = await service.GetPresetsAsync(deviceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("standard", result[0].presetName);
        Assert.Same(expectedPresets, result);

    }
    [Fact]
    public async Task GetUserDevicesAsync_WithValidUserId_ReturnsExpectedUserDevices()
    {
        // Arrange
        var mockRepo = new Mock<ICreatePlanRepository>();
        string userId = "c76c5a71-0b33-4a63-9ad0-0d749c72d324";

        var expectedUserDevices = new List<PlanUserDeviceDto>
        {
            new PlanUserDeviceDto(
                48,
                34,
                "EV1",
                "eEvCharger",
                "#c50bea",
                "Interruptible",
                "EV1",
                400.000m)
        };
        

        mockRepo.Setup(repo => repo.GetUserDevicesAsync(userId))
            .Returns(Task.FromResult(expectedUserDevices));

        var service = new CreatePlanService(mockRepo.Object);

        // Act
        var result = await service.GetUserDevicesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400.000m, result[0].batteryCapacityInKwh);
        Assert.Same(expectedUserDevices, result);

    }

    [Fact]
    public async Task PostPlanAsync_WithValidPlan_ReturnsCreatedPlan()
    {
        // Arrange
        var mockRepo = new Mock<ICreatePlanRepository>();
        
        PlanDTO planDTO = new PlanDTO
        {
            userDeviceId = 34,
            startTime = DateTime.UtcNow,
            endTime = DateTime.UtcNow.AddHours(2),
            windowStartTime = DateTime.UtcNow,
            windowEndTime = DateTime.UtcNow,
            estimatedCost = 15.5m,
            estimatedCo2Emissions = 1.2m,
            estimatedUnscheduledCost = 25.0m,
            estimatedUnscheduledCo2Emissions = 2.5m,
            planName = "EV Charging Plan",
            optimizationObjective = "Cheapest",
        };
        
        var expectedPlan = new PlanDTO
        {
            userDeviceId = 34,
            startTime = planDTO.startTime,
            endTime = planDTO.endTime,
            windowStartTime = planDTO.windowStartTime,
            windowEndTime = planDTO.windowEndTime,
            estimatedCost = planDTO.estimatedCost,
            estimatedCo2Emissions = planDTO.estimatedCo2Emissions,
            estimatedUnscheduledCost = planDTO.estimatedUnscheduledCost,
            estimatedUnscheduledCo2Emissions = planDTO.estimatedUnscheduledCo2Emissions,
            planName = planDTO.planName,
            optimizationObjective = planDTO.optimizationObjective,
            batteryCapacityInKwh = 400.000m
        };

        mockRepo.Setup(repo => repo.PostPlanAsync(planDTO))
            .Returns(Task.FromResult(expectedPlan));

        var service = new CreatePlanService(mockRepo.Object);

        // Act
        var result = await service.PostPlanAsync(planDTO);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400.000m, result.batteryCapacityInKwh);
        Assert.Same(expectedPlan, result);

    }

}
