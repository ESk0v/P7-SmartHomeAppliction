using Microsoft.AspNetCore.Identity;
using SmartHomeApplicationAPI.DTOs;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace SmartHomeApplicationAPI.Controller
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        // private readonly ILogger<AuthController> _logger; ------> Will be used later
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthRespDTO>> Register([FromBody] RegisterReqDTO request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var username = request.Username;
            var email = request.Email;

            var existingUsername = await _userManager.FindByNameAsync(username);
            if (existingUsername is not null)
            {
                return Conflict(new
                {
                    Message = "Did not complete registration of user. Username error"
                });
            }

            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail is not null)
            {
                return Conflict(new
                {
                    Message = "Did not complete registration of user. Email error"
                });
            }

            var user = new User
            {
                UserName = username,
                Email = email,
                DisplayName = request.DisplayName,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    Errors = result.Errors.Select(error => error.Description)
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthRespDTO(user.Id, user.UserName, user.DisplayName, roles.ToArray()));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthRespDTO>> Login([FromBody] LoginReqDTO request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user is null)
            {
                return NotFound();
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthRespDTO(user.Id, user.UserName!, user.DisplayName, roles.ToArray()));
        }
    }
}
