using Moq;
using Xunit;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Service;

public class PlanServiceTests
{
    [Fact]
    public async Task GetPlansAsync_WithValidUserId_ReturnsExpectedPlans()
    {
        // Arrange
        var mockRepo = new Mock<IPlanRepository>();
        string testUserId = Guid.NewGuid().ToString();

        // Act
        var expectedPlans = new List<PlanDto>
        {
            new PlanDto(
                planId: 1,
                nextPlanId: null,
                planName: "Natopladning",
                icon: "bolt",
                colorCode: "#FF5733",
                deviceName: "Elbil",
                startTime: DateTime.UtcNow,
                endTime: DateTime.UtcNow.AddHours(2),
                estimatedCost: 15.5m,
                estimatedCo2Emissions: 1.2m
            )
        };

        mockRepo.Setup(repo => repo.GetPlansAsync(testUserId, It.IsAny<DateTime>()))
            .Returns(Task.FromResult(expectedPlans));

        var service = new PlanService(mockRepo.Object);

        // Act
        var result = await service.GetPlansAsync(testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Natopladning", result[0].planName);
        Assert.Same(expectedPlans, result);
    }
}
