using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHomeApplicationAPI.Controllers;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;
using Xunit;

namespace SmartHomeApplicationAPI.Tests.Controller
{
    public class DeviceControllerTests
    {
        private readonly Mock<IDeviceService> _mockService;
        private readonly DeviceController _controller;

        public DeviceControllerTests()
        {
            // Mock af servicen som skal genbruges i alle tests
            _mockService = new Mock<IDeviceService>();
            _controller = new DeviceController(_mockService.Object);
        }

        // Controlleren returnerer anonyme objekter i Ok(new { ... }), denne function bruges til at hente værdierne fra de anonyme objekter i testene
        private static object? GetProperty(object? value, string name)
        {
            return value!.GetType().GetProperty(name)!.GetValue(value);
        }

        [Fact]
        public async Task UpdateDevice_WhenServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var request = new UpdateUserDeviceDto { Id = 1 };

            _mockService.Setup(s => s.UpdateUserDeviceAsync(request)).ThrowsAsync(new ArgumentException("Invalid device."));

            // Act
            var result = await _controller.UpdateDevice(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
            Assert.Equal("Invalid device.", badRequest.Value);
        }

        [Fact]
        public async Task UpdateDevice_WhenDeviceNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateUserDeviceDto { Id = 1 };

            _mockService.Setup(s => s.UpdateUserDeviceAsync(request)).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateDevice(request);

            // Assert
            var notFound = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }

        [Fact]
        public async Task UpdateDevice_WhenValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new UpdateUserDeviceDto { Id = 1 };

            _mockService.Setup(s => s.UpdateUserDeviceAsync(request)).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateDevice(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, GetProperty(okResult.Value, "id"));
            Assert.Equal("Device updated successfully", GetProperty(okResult.Value, "message"));
            _mockService.Verify(s => s.UpdateUserDeviceAsync(request), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetUserDevices_WhenUserIdIsMissing_ReturnsUnauthorized(string? userId)
        {
            // Act
            var result = await _controller.GetUserDevices(userId!);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
            _mockService.Verify(s => s.GetUserDevicesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetUserDevices_WhenUserIdIsProvided_ReturnsOkWithDevices()
        {
            // Arrange
            const string userId = "user-1";
            var devices = new List<UserDeviceDto>();

            _mockService.Setup(s => s.GetUserDevicesAsync(userId)).ReturnsAsync(devices);

            // Act
            var result = await _controller.GetUserDevices(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(devices, okResult.Value);
            _mockService.Verify(s => s.GetUserDevicesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetAllDevices_ReturnsOkWithDevices()
        {
            // Arrange
            var devices = new List<CatalogueDeviceDto>();

            _mockService.Setup(s => s.GetAllDevicesAsync()).ReturnsAsync(devices);

            // Act
            var result = await _controller.GetAllDevices();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(devices, okResult.Value);
        }

        [Fact]
        public async Task Create_WhenCapacityConflicts_ReturnsConflict()
        {
            // Arrange
            var request = new CreateDeviceDto();
            var exception = new DeviceCapacityConflictException();

            _mockService.Setup(s => s.CreateDeviceAsync(request)).ThrowsAsync(exception);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
            Assert.Equal(exception.Message, conflictResult.Value);
        }

        [Fact]
        public async Task Create_WhenValidRequest_ReturnsOkWithId()
        {
            // Arrange
            var request = new CreateDeviceDto();

            _mockService.Setup(s => s.CreateDeviceAsync(request)).ReturnsAsync(42);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(42, GetProperty(okResult.Value, "id"));
            Assert.Equal("Device created successfully", GetProperty(okResult.Value, "message"));
            _mockService.Verify(s => s.CreateDeviceAsync(request), Times.Once);
        }

        [Fact]
        public async Task UpdatePreset_WhenDeviceNotFound_ReturnsNotFound()
        {
            // Arrange
            const int userDeviceId = 3;
            var request = new PresetDeviceDto();

            _mockService.Setup(s => s.UpdatePresetAsync(userDeviceId, request)).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdatePreset(userDeviceId, request);

            // Assert
            var notFound = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }

        [Fact]
        public async Task UpdatePreset_WhenValidRequest_ReturnsOk()
        {
            // Arrange
            const int userDeviceId = 3;
            var request = new PresetDeviceDto();

            _mockService.Setup(s => s.UpdatePresetAsync(userDeviceId, request)).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdatePreset(userDeviceId, request);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockService.Verify(s => s.UpdatePresetAsync(userDeviceId, request), Times.Once);
        }

        [Fact]
        public async Task CreatePreset_WhenDeviceNotFound_ReturnsNotFound()
        {
            // Arrange
            const int userDeviceId = 3;
            var request = new PresetDeviceDto();

            _mockService.Setup(s => s.CreatePresetAsync(userDeviceId, request)).ReturnsAsync(false);

            // Act
            var result = await _controller.CreatePreset(userDeviceId, request);

            // Assert
            var notFound = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }

        [Fact]
        public async Task CreatePreset_WhenValidRequest_ReturnsOkWithPreset()
        {
            // Arrange
            const int userDeviceId = 3;
            var request = new PresetDeviceDto { PresetName = "Night" };

            _mockService.Setup(s => s.CreatePresetAsync(userDeviceId, request)).ReturnsAsync(true);

            // Act
            var result = await _controller.CreatePreset(userDeviceId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(request, okResult.Value);
            _mockService.Verify(s => s.CreatePresetAsync(userDeviceId, request), Times.Once);
        }

        [Fact]
        public async Task DeleteUserDevice_WhenDeviceNotFound_ReturnsNotFound()
        {
            // Arrange
            const int userDeviceId = 3;

            _mockService.Setup(s => s.DeleteUserDeviceAsync(userDeviceId)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteUserDevice(userDeviceId);

            // Assert
            var notFound = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }

        [Fact]
        public async Task DeleteUserDevice_WhenDeviceExists_ReturnsNoContent()
        {
            // Arrange
            const int userDeviceId = 3;

            _mockService.Setup(s => s.DeleteUserDeviceAsync(userDeviceId)).ReturnsAsync(true);
            _mockService.Setup(s => s.DeleteUserDeviceAsync(userDeviceId)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUserDevice(userDeviceId);

            // Assert
            var noContent = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
            _mockService.Verify(s => s.DeleteUserDeviceAsync(userDeviceId), Times.Once);
        }
    }
}