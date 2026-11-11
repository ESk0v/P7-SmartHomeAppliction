using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartHomeApplicationAPI.Controller;
using SmartHomeApplicationAPI.DTOs;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Controller.Dashboard;
using Xunit;
using System.Net;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Controller
{
    public class CreatePlanControllerTests
    {
        private readonly Mock<ILogger<CreatePlanController>> _mockLogger;
        private readonly Mock<ICreatePlanService> _mockService;

        public CreatePlanControllerTests()
        {
            _mockLogger = new Mock<ILogger<CreatePlanController>>();
            _mockService = new Mock<ICreatePlanService>();
        }

        [Fact]
        public async Task GetUserDevices_WhenUserIdIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var controller = new CreatePlanController(
                _mockService.Object,
                _mockLogger.Object);

            var userId = "";

            // Act
            var result = await controller.GetUserDevices(userId);

            // Assert
            var objectResult =
                Assert.IsType<BadRequestObjectResult>(result.Result);

            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            Assert.Equal("Invalid or missing User ID.", objectResult.Value);
            _mockService.Verify(s => s.GetUserDevicesAsync(It.IsAny<string>()), Times.Never);

        }

        [Fact]
        public async Task GetUserDevices_WhenUserIdIsValid_ReturnsOk()
        {
            // Arrange
            var controller = new CreatePlanController(
                _mockService.Object,
                _mockLogger.Object);

            var userId = "c76c5a71-0b33-4a63-9ad0-0d749c72d324";

            // Act
            var result = await controller.GetUserDevices(userId);

            // Assert
            var objectResult =
                Assert.IsType<OkObjectResult>(result.Result);

            Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
            _mockService.Verify(s => s.GetUserDevicesAsync(It.IsAny<string>()), Times.Once);

        }

        [Fact]
        public async Task PostPlan_WhenPlanIsNull_ReturnsBadRequest()
        {
            // Arrange
            var controller = new CreatePlanController(
                _mockService.Object,
                _mockLogger.Object);

            PlanDTO planDTO = null!;

            // Act
            var result = await controller.PostPlan(planDTO);

            // Assert
            var objectResult =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            Assert.Equal("Plan is required.", objectResult.Value);
            _mockService.Verify(s => s.PostPlanAsync(It.IsAny<PlanDTO>()), Times.Never);

        }

        [Fact]
        public async Task PostPlan_WhenPlanValid_ReturnsOk()
        {
            // Arrange
            var controller = new CreatePlanController(
                _mockService.Object,
                _mockLogger.Object);

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

            // Act
            var result = await controller.PostPlan(planDTO);

            // Assert
            var objectResult =
                Assert.IsType<OkObjectResult>(result);

            Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
            _mockService.Verify(s => s.PostPlanAsync(It.IsAny<PlanDTO>()), Times.Once);

        }
      
    }
}
