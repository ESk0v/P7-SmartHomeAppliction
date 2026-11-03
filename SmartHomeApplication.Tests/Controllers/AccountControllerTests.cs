using System.ComponentModel;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using SmartHomeApplication.Controllers;
using SmartHomeApplication.Models;
using SmartHomeApplication.Tests.Helpers;
using Xunit;

namespace SmartHomeApplication.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<ILogger<AccountController>> _mockLogger;

        public AccountControllerTests()
        {
            // Setup logger for Making AuthController in each test
            _mockLogger = new Mock<ILogger<AccountController>>();
        }

        [Fact]
        public async Task Register_WhenBackendReturnsConflict_RedirectsWithErrorUnavailable()
        {
            // Arrange: Backend returns 409 Conflict
            var (mockClient, _) = HttpClientTestHelper.CreateMockHttpClient(HttpStatusCode.Conflict);
            var controller = new AccountController(mockClient, _mockLogger.Object);

            var model = new RegisterViewModel
            {
                Username = "LasseUser",
                Email = "test@test.com",
                Password = "SecurePassword123!",
                PasswordConfirm = "SecurePassword123!",
                DisplayName = "LasseDisplay"
            };

            // Act
            var result = await controller.Register(model, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/register?error=unavailable", redirectResult.Url);
        }

        [Fact]
        public async Task Register_WhenRepsonseCodeIsNotSuccess_RedirectsWithErrorInvalid()
        {
            // Arrange
            var (mockClient, _) = HttpClientTestHelper.CreateMockHttpClient(HttpStatusCode.BadRequest);
            var controller = new AccountController(mockClient, _mockLogger.Object);

            var model = new RegisterViewModel
            {
                Username = "LasseTest",
                Email = "test@test.com",
                Password = "SecurePassword123!",
                PasswordConfirm = "SecurePassword123!",
                DisplayName = "LasseDisplay"
            };

            // Act
            var result = await controller.Register(model, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/register?error=invalid", redirectResult.Url);
        }

        [Fact]
        public async Task Register_WhenSuccessfull_RedirectWithParameterRegisteredTrue()
        {
            // Arrange
            var (mockClient, _) = HttpClientTestHelper.CreateMockHttpClient(HttpStatusCode.OK);
            var controller = new AccountController(mockClient, _mockLogger.Object);

            var model = new RegisterViewModel
            {
                Username = "LasseTest",
                Email = "test@test.com",
                Password = "SecurePassword123!",
                PasswordConfirm = "SecurePassword123!",
                DisplayName = "LasseDisplay"
            };

            // Act
            var result = await controller.Register(model, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/login?registered=true", redirectResult.Url);
        }
    }
}
