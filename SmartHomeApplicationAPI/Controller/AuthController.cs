using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SmartHomeApplicationAPI.DTOs;
using System.Security.Claims;

namespace SmartHomeApplicationAPI.Controller
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            if (model.Username == "admin" && model.Password == "password")
            {
                /*
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                */
                return Ok(new
                {
                    IsAuthenticated = true,
                    UserName = model.Username
                });
            }

            return Redirect("/login?error=InvalidUsernameOrPassword");
        }

        [HttpGet("user-info")]
        public IActionResult GetUserInfo()
        {
            if (!User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            return Ok(new { UserName = User.Identity?.Name, IsAuthenticated = true });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect("/login");
        }
    }
}
