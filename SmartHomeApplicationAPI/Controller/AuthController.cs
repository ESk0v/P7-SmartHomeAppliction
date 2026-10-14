using Microsoft.AspNetCore.Identity;
using SmartHomeApplicationAPI.DTOs;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace SmartHomeApplicationAPI.Controller
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthRespDTO>> Register([FromBody] RegisterReqDTO request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Beginning registration flow in backend");
            
            cancellationToken.ThrowIfCancellationRequested();

            var username = request.Username;
            var email = request.Email;

            _logger.LogInformation("Attempting to find user by Username. If found, Conflict is returned");
            var existingUsername = await _userManager.FindByNameAsync(username);
            if (existingUsername is not null)
            {
                _logger.LogInformation("Username already found in DB. Returning Conflict");
                return Conflict(new
                {
                    Message = "Did not complete registration of user. Username error"
                });
            }

            _logger.LogInformation("Attempting to find email. If found, Conflict is returned");
            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail is not null)
            {
                _logger.LogInformation("Email already found in DB. Returning Conflict");
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

            _logger.LogInformation("Creating new user");
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    Errors = result.Errors.Select(error => error.Description)
                });
            }
            _logger.LogInformation("New user created. It took {TotalSeconds}", stopwatch.Elapsed.TotalSeconds);

            var roles = await _userManager.GetRolesAsync(user);
            
            _logger.LogInformation("Registration finished");
            stopwatch.Stop();

            return Ok(new AuthRespDTO(user.Id, user.UserName, user.DisplayName, roles.ToArray()));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthRespDTO>> Login([FromBody] LoginReqDTO request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Beginning login flow in backend");

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Searching for user by Username through the request");
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user is null)
            {
                _logger.LogWarning("User not found");
                return NotFound();
            }

            _logger.LogInformation("Validating password");
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("3 failed attempts to login. User locked out.");
                    return StatusCode(StatusCodes.Status423Locked);
                }

                _logger.LogInformation("Failed login attempt, unauthorized");
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("Login finished successfully");
            return Ok(new AuthRespDTO(user.Id, user.UserName!, user.DisplayName, roles.ToArray()));
        }
    }
}
