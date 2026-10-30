using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;

namespace SmartHomeApplicationAPI.Controllers;

[ApiController]
[Route("api/device-controller")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _service;

    public DeviceController(IDeviceService service)
    {
        _service = service;
    }
    
    [HttpPut("update-userdevice")]
    public async Task<IActionResult> UpdateDevice([FromBody] UpdateUserDeviceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _service.UpdateUserDeviceAsync(dto);

        if (!result)
        {
            return NotFound();
        }

        return Ok(new { message = "Device updated successfully", id = dto.Id });
    }

    [HttpGet("get-userdevices")]
    public async Task<IActionResult> GetUserDevices([FromHeader(Name = "User-Id")] string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var userDevices = await _service.GetUserDevicesAsync(userId);
        return Ok(userDevices);
    }

    [HttpGet("get-all-devices")]
    public async Task<IActionResult> GetAllDevices()
    {
        var devices = await _service.GetAllDevicesAsync();
        return Ok(devices);
    }

    [HttpPost("create-device")]
    public async Task<IActionResult> Create([FromBody] CreateDeviceDto dto)
    {
        if (!ModelState.IsValid) { 
            return ValidationProblem(ModelState);
        }

        var deviceId = await _service.CreateDeviceAsync(dto);
        return Ok(new { message = "Device created successfully", id = deviceId });
    }
}
