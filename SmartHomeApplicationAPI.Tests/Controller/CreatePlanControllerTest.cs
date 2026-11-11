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
      
    }
}
