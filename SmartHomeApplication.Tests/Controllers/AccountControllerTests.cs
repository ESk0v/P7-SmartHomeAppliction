using System.ComponentModel;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
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

        [Fact]
        public async Task Login_WhenAccountIsLocked_RedirectsWithErrorLockoutBegin()
        {
            // Arrange
            var (mockClient, _) = HttpClientTestHelper.CreateMockHttpClient(HttpStatusCode.Locked);
            var controller = new AccountController(mockClient, _mockLogger.Object);

            string username = "LasseUser";
            string password = "SecretPassword123";

            // Act
            var result = await controller.Login(username, password, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/login?error=lockoutBegin", redirectResult.Url);
        }

        [Fact]
        public async Task Login_WhenRepsonseCodeIsNotSuccess_RedirectsWithErrorInvalidLogin()
        {
            // Arrange
            var (mockClient, _) = HttpClientTestHelper.CreateMockHttpClient(HttpStatusCode.BadRequest);
            var controller = new AccountController(mockClient, _mockLogger.Object);

            string username = "LasseUser";
            string password = "SecretPassword123";

            // Act
            var result = await controller.Login(username, password, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/login?error=invalidLogin", redirectResult.Url);
        }

        [Fact]
        public async Task Login_WhenResultIsNull_ReturnsStatusCode502()
        {
            // Arrange
            var (mockClient, _) = HttpClientTestHelper.CreateMockHttpClient(HttpStatusCode.OK, "null");
            var controller = new AccountController(mockClient, _mockLogger.Object);

            string username = "LasseUser";
            string password = "SecretPassword123";
            
            // Act
            var result = await controller.Login(username, password, CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
            Assert.Equal("Auth is invalid", objectResult.Value);
        }

        [Fact]
        public async Task Login_WhenSuccessful_RedirectsToDashboard()
        {
            // Arrange
            var authModel = new AuthRespModel("123", "LasseUser", "LasseDisplay", new[] {"User"});
            var jsonPayload = JsonSerializer.Serialize(authModel);

            var (mockClient, _) = HttpClientTestHelper.CreateMockHttpClient(HttpStatusCode.OK, jsonPayload);
            var controller = new AccountController(mockClient, _mockLogger.Object);

            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService
                .Setup(a => a.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthService.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = mockServiceProvider.Object
                }
            };

            string username = "LasseUser";
            string password = "SecretPassword123";

            // Act
            var result = await controller.Login(username, password, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/", redirectResult.Url);

            mockAuthService.Verify(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(cp => cp.Identity!.Name == "LasseUser"),
                It.IsAny<AuthenticationProperties>()
            ), Times.Once);
        }

        [Fact]
        public async Task Logout_WhenCalled_SignsOutAndRedirectsToLogin()
        {
            // Arrange
            var (mockClient, _) = HttpClientTestHelper.CreateMockHttpClient(HttpStatusCode.OK);
            var controller = new AccountController(mockClient, _mockLogger.Object);

            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService
                .Setup(a => a.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthService.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = mockServiceProvider.Object
                }
            };

            // Act
            var result = await controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/login", redirectResult.Url);

            mockAuthService.Verify(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties>()
            ), Times.Once);
        }
    }
}
