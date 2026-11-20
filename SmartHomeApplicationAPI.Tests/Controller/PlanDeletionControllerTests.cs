using Controller.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHomeApplicationAPI.Service;

namespace SmartHomeApplicationAPI.Tests.Controller;

public class PlanDeletionControllerTests
{
    [Theory]
    [InlineData(true, 204)]
    [InlineData(false, 404)]
    public async Task Delete_PassesUserAndPlanToService_AndReturnsTheResult(bool deleted, int status)
    {
        var service = new Mock<IPlanService>();
        service.Setup(value => value.DeletePlanAsync("owner", 123)).ReturnsAsync(deleted);
        var controller = new PlanController(service.Object);

        var result = Assert.IsAssignableFrom<StatusCodeResult>(await controller.DeletePlan(123, "owner"));

        Assert.Equal(status, result.StatusCode);
        service.Verify(value => value.DeletePlanAsync("owner", 123), Times.Once);
    }

    [Fact]
    public async Task Delete_RequiresAUserId()
    {
        var service = new Mock<IPlanService>(MockBehavior.Strict);
        var controller = new PlanController(service.Object);

        Assert.IsType<UnauthorizedResult>(await controller.DeletePlan(123, " "));
    }
}
