using Controller.Dashboard;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Controller;

public class PlanReplacementControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(10)]
    public async Task Save_PassesAllIntervalsAndOwnerToTheService(int? originalId)
    {
        var request = new SaveGeneratedPlanRequestDto { ReplacesPlanId = originalId };
        var saved = new PlanDTO { planId = 20 };
        var service = new Mock<IPlanService>();
        service.Setup(value => value.SaveGeneratedPlanAsync("owner", request)).ReturnsAsync(saved);

        var result = await new PlanController(service.Object).SaveGeneratedPlan("owner", request);

        Assert.Same(saved, Assert.IsType<OkObjectResult>(result.Result).Value);
        service.Verify(value => value.SaveGeneratedPlanAsync("owner", request), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Replace_ReturnsNotFoundOrValidationError(bool invalid)
    {
        var request = new SaveGeneratedPlanRequestDto { ReplacesPlanId = 10 };
        var service = new Mock<IPlanService>();
        if (invalid) service.Setup(value => value.SaveGeneratedPlanAsync("owner", request))
            .ThrowsAsync(new ValidationException("Replacement is outside the window."));

        var result = await new PlanController(service.Object).SaveGeneratedPlan("owner", request);

        if (invalid) Assert.Equal("Replacement is outside the window.", Assert.IsType<BadRequestObjectResult>(result.Result).Value);
        else Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Save_RejectsMissingOwnerBeforeCallingService()
    {
        var controller = new PlanController(new Mock<IPlanService>(MockBehavior.Strict).Object);
        Assert.IsType<UnauthorizedResult>((await controller.SaveGeneratedPlan(" ", new())).Result);
    }
}
