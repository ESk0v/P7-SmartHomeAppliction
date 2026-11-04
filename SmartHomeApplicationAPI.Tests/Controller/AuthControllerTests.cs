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
using Xunit;

namespace SmartHomeApplicationAPI.Tests.Controller
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // Mocks for userManager og signInManager which needs to be re-used
            var userStore = new Mock<IUserStore<User>>();

            _mockUserManager = new Mock<UserManager<User>>(
            userStore.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());

            _mockSignInManager = new Mock<SignInManager<User>>(
            _mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<User>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<User>>());

            // Setup logger for Making AuthController
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockUserManager.Object, _mockSignInManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Register_WhenUsernameAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = new RegisterReqDTO
            {
                Username = "Lasse",
                Email = "test@test.com",
                Password = "password123",
                PasswordConfirm = "password123",
                DisplayName = "LasseTest"
            };

            _mockUserManager.Setup(u => u.FindByNameAsync(request.Username)).ReturnsAsync(new User { UserName = request.Username});

            // Act
            var result = await _controller.Register(request, CancellationToken.None);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }

        [Fact]
        public async Task Register_Failed_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterReqDTO
            {
                Username = "Lasse",
                Email = "test@test.com",
                Password = "password123",
                PasswordConfirm = "password123",
                DisplayName = "LasseTest"
            };
            var user = new User { DisplayName = request.Username };

            _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<User>(), request.Password)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "bad" }));

            // Act
            var result = await _controller.Register(request, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        }


        [Fact]
        public async Task Register_WhenValidRequest_CreatesUserAndReturnsOk()
        {
            // Arrange
            var request = new RegisterReqDTO
            {
                Username = "Lasse",
                Email = "test@test.com",
                Password = "password123",
                PasswordConfirm = "password123",
                DisplayName = "LasseTest"
            };
            var expectedRoles = new[] { "User" };

            // Mocker all kald der sker inde i controller inden happy path nåes
            _mockUserManager.Setup(u => u.FindByNameAsync(request.Username)).ReturnsAsync((User?)null);
            _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<User>(), request.Password)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(expectedRoles);

            // Act
            var result = await _controller.Register(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseData = Assert.IsType<AuthRespDTO>(okResult.Value);

            Assert.Equal(request.Username, responseData.Username);
            Assert.Equal(request.DisplayName, responseData.DisplayName);
            Assert.Contains("User", responseData.Roles);
        }

        [Fact]
        public async Task Login_WhenUserNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new LoginReqDTO
            {
                Username = "unknownUser",
                Password = "Secret"
            };

            _mockUserManager.Setup(u => u.FindByNameAsync(request.Username)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Login(request, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Login_WhenUserLockedOut_ReturnsStatusCode423()
        {
            // Arrange
            var request = new LoginReqDTO
            {
                Username = "lockedUser",
                Password = "Secret1234"
            };
            var user = new User { DisplayName = request.Username };

            _mockUserManager.Setup(u => u.FindByNameAsync(request.Username)).ReturnsAsync(user);

            // If i do not use entire <Microsoft.AspNetCore.Identity> here, i get conflict on names with .MVC from Microsoft
            _mockSignInManager.Setup(u => u.CheckPasswordSignInAsync(user, request.Password, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            // Act
            var result = await _controller.Login(request, CancellationToken.None);

            // Assert
            var statusresult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(StatusCodes.Status423Locked, statusresult.StatusCode);
        }

        [Fact]
        public async Task Login_WhenUserUnauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginReqDTO
            {
                Username = "lockedUser",
                Password = "Secret1234"
            };
            var user = new User { DisplayName = request.Username };

            _mockUserManager.Setup(u => u.FindByNameAsync(request.Username)).ReturnsAsync(user);

            // If i do not use entire <Microsoft.AspNetCore.Identity> here, i get conflict on names with .MVC from Microsoft
            _mockSignInManager.Setup(u => u.CheckPasswordSignInAsync(user, request.Password, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(request, CancellationToken.None);

            // Assert
            var statusresult = Assert.IsType<UnauthorizedResult>(result.Result);
            Assert.Equal(StatusCodes.Status401Unauthorized, statusresult.StatusCode);
        }

        [Fact]
        public async Task Login_WhenUserIsAuthorized_ReturnsOk()
        {
            // Arrange
            var request = new LoginReqDTO
            {
                Username = "Success",
                Password = "YeahYeah1"
            };
            var user = new User { DisplayName = request.Username };
            var role = new[] { "User" };

            // Mocker all kald der sker inde i controller inden happy path nåes
            _mockUserManager.Setup(u => u.FindByNameAsync(request.Username)).ReturnsAsync(user);
            _mockSignInManager.Setup(u => u.CheckPasswordSignInAsync(user, request.Password, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(role);

            // Act
            var result = await _controller.Login(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthRespDTO>(okResult.Value);

            Assert.Equal(user.Id, response.UserId);
            Assert.Equal(user.UserName, response.Username);
            Assert.Equal(user.DisplayName, response.DisplayName);
            Assert.Equal(role, response.Roles);
        }

        [Fact]
        public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = new RegisterReqDTO
            {
                Username = "Lasse",
                Email = "test@test.com",
                Password = "password123",
                PasswordConfirm = "password123",
                DisplayName = "LasseTest"
            };

            _mockUserManager
                .Setup(u => u.FindByNameAsync(request.Username))
                .ReturnsAsync((User?)null);

            _mockUserManager
                .Setup(u => u.FindByEmailAsync(request.Email))
                .ReturnsAsync(new User { Email = request.Email });

            // Act
            var result = await _controller.Register(request, CancellationToken.None);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }
    }
}
