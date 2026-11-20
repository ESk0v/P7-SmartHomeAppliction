using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SmartHomeApplication.Controllers;

namespace SmartHomeApplication.Tests.Controllers;

public class SessionControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CheckingSession_NeverRenewsAuthentication(bool signedIn)
    {
        var (controller, authentication, _) = CreateController(signedIn);

        var result = await controller.Session();

        if (signedIn) Assert.IsType<JsonResult>(result);
        else Assert.IsType<UnauthorizedResult>(result);
        authentication.Verify(service => service.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
            It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContinueSession_RenewsOnlyAnAuthenticatedUser_UsingConfiguredLifetime(bool signedIn)
    {
        var (controller, authentication, principal) = CreateController(signedIn);
        var options = new Mock<IOptionsMonitor<CookieAuthenticationOptions>>();
        options.Setup(value => value.Get(CookieAuthenticationDefaults.AuthenticationScheme))
            .Returns(new CookieAuthenticationOptions { ExpireTimeSpan = TimeSpan.FromMinutes(30) });
        AuthenticationProperties? renewed = null;
        authentication.Setup(service => service.SignInAsync(It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme, principal, It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, _, properties) => renewed = properties)
            .Returns(Task.CompletedTask);

        var result = await controller.ContinueSession(options.Object);

        if (!signedIn)
        {
            Assert.IsType<UnauthorizedResult>(result);
            Assert.Null(renewed);
            return;
        }
        Assert.IsType<JsonResult>(result);
        Assert.NotNull(renewed);
        Assert.Equal(TimeSpan.FromMinutes(30), renewed.ExpiresUtc - renewed.IssuedUtc);
        Assert.True(renewed.IsPersistent);
        Assert.InRange(renewed.IssuedUtc!.Value, DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow);
    }

    private static (AccountController Controller, Mock<IAuthenticationService> Authentication, ClaimsPrincipal Principal) CreateController(bool signedIn)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Test user") }, "Cookies"));
        var properties = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(1) };
        var result = signedIn
            ? AuthenticateResult.Success(new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme))
            : AuthenticateResult.NoResult();
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(result);
        var services = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();
        var controller = new AccountController(new HttpClient(), NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };
        return (controller, authentication, principal);
    }
}
