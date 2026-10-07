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

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] string username, string password, CancellationToken cancellationToken)
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
                return StatusCode(StatusCode.Status502BadGateway, "Auth is invalid");
            }

            await SignInAsync(result);

            return Redirect("/");
        }

        
        
    }
}
