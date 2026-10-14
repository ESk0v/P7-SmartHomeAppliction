using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using SmartHomeApplication.Models;
using System.Diagnostics;
using System.Net;


namespace SmartHomeApplication.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly HttpClient _apiClient;
        private readonly ILogger<AccountController> _logger;

        public AccountController(HttpClient apiClient, ILogger<AccountController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromForm] RegisterViewModel model, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting registration flow");
            
            var response = await _apiClient.PostAsJsonAsync("api/auth/register", model, cancellationToken);
            _logger.LogInformation("Registration returned after: {TotalSeconds} seconds", stopwatch.Elapsed.TotalSeconds);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Registration failed with error unavailable");
                return Redirect("/register?error=unavailable");
            }
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Registration failed with error invalid");
                return Redirect("/register?error=invalid");
            }

            _logger.LogInformation("Registration completed. New user created");
            stopwatch.Stop();

            return Redirect("/login?registered=true");
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting login flow");

            using var response = await _apiClient.PostAsJsonAsync("api/auth/login", new
            {
                Username = username,
                Password = password
            }, 
            cancellationToken);
            _logger.LogInformation("Login response returned after: {elapsedSecondsRegister} seconds", stopwatch.Elapsed.TotalSeconds);

            // Http.Locked is statuscode 423
            if (response.StatusCode == HttpStatusCode.Locked)
            {
                _logger.LogWarning("Account locked due to 3 invalid login attempts");
                return Redirect("/login?error=lockoutBegin");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Invalid login attempt");
                return Redirect("/login?error=invalidLogin");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthRespModel>(cancellationToken);
            
            if (result is null)
            {
                _logger.LogWarning("Invalid authentication");
                return StatusCode(StatusCodes.Status502BadGateway, "Auth is invalid");
            }
            
            _logger.LogInformation("Signing in...");
            await SignInAsync(result);
            _logger.LogInformation("Signed in successfull. Total flow took: {TotalSeconds}", stopwatch.Elapsed.TotalSeconds);
            stopwatch.Stop();

            return Redirect("/");
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Logout requested");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Logout successfull. Request took {TotalSeconds}.", stopwatch.Elapsed.TotalSeconds);
            stopwatch.Stop();

            return Redirect("/login");
        }

        private async Task SignInAsync(AuthRespModel result)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Inside SignInAsync method");
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId),
                new(ClaimTypes.Name, result.Username),
                new("display_name", result.DisplayName)
            };
            _logger.LogInformation("New claims created");

            claims.AddRange(
                result.Roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

            _logger.LogInformation("Creating identity...");
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Creating principal...");
            var principal = new ClaimsPrincipal(identity);

            _logger.LogInformation("SignInAsync called");
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            _logger.LogInformation("SignInAsync returned after: {TotalSeconds}", stopwatch.Elapsed.TotalSeconds);
            stopwatch.Stop();
        }
        
    }
}
