using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using SmartHomeApplication.Models;


namespace SmartHomeApplication.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly HttpClient _apiClient;

        public AccountController(HttpClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromForm] RegisterViewModel model, CancellationToken cancellationToken)
        {
            var response = await _apiClient.PostAsJsonAsync("api/auth/register", model, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Redirect("/register?error=unavailable");
            }
            if (!response.IsSuccessStatusCode)
            {
                return Redirect("/register?error=invalid");
            }

            return Redirect("/login?registered=true");
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, CancellationToken cancellationToken)
        {
            using var response = await _apiClient.PostAsJsonAsync("api/auth/login", new
            {
                Username = username,
                Password = password
            }, 
            cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Redirect("/login?error=invalidLogin");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthRespModel>(cancellationToken);
            
            if (result is null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "Auth is invalid");
            }

            await SignInAsync(result);

            return Redirect("/");
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/login");
        }

        private async Task SignInAsync(AuthRespModel result)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId),
                new(ClaimTypes.Name, result.Username),
                new("display_name", result.DisplayName)
            };

            claims.AddRange(
                result.Roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
        
    }
}
