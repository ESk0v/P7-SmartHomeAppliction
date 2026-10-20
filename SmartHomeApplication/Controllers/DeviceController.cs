using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHomeApplication.Models;
using System.Security.Claims;
using Utilities.Models;

namespace SmartHomeApplication.Controllers
{
    [Route("device-controller")]    
    public class DeviceController : Controller
    {
        private readonly HttpClient _httpClient;

        private readonly ILogger<DeviceController> _logger;

        public DeviceController(HttpClient httpClient, ILogger<DeviceController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("create-device")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDevice([FromBody] DevicesDto deviceDto, CancellationToken cancellationToken)
        {
            _logger.LogError("CreateDevice called with deviceDto: {@DeviceDto}", deviceDto);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Should handle if null

            _logger.LogError("UserId: {UserId}", userId);
            deviceDto.UserId = userId!;

            using var response = await _httpClient.PostAsJsonAsync("api/device-controller/create-device", deviceDto);
            _logger.LogError("Response status code: {StatusCode}", response.StatusCode);

            // Should handle status checks.

            var result = await response.Content.ReadFromJsonAsync<DevicesDto>(cancellationToken);
            _logger.LogError("Result from API: {@Result}", result);

            // Chek if result is null and handle accordingly

            if (result == null)
            {
                // Handle the case where the result is null
                return BadRequest("Failed to create device");
            }

            return Redirect("/addAppliance");
        }

    }
}