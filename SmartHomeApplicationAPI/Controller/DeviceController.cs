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

        bool result;
        try
        {
            result = await _service.UpdateUserDeviceAsync(dto);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }

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

        int deviceId;
        try
        {
            deviceId = await _service.CreateDeviceAsync(dto);
        }
        catch (DeviceCapacityConflictException exception)
        {
            return Conflict(exception.Message);
        }
        return Ok(new { message = "Device created successfully", id = deviceId });
    }

    [HttpPut("{userDeviceId:int}/preset")]
    public async Task<IActionResult> UpdatePreset(int userDeviceId, [FromBody] PresetDeviceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await _service.UpdatePresetAsync(userDeviceId, dto))
        {
            return NotFound();
        }

        return Ok();
    }

    [HttpPost("{userDeviceId:int}/preset")]
    public async Task<IActionResult> CreatePreset(int userDeviceId, [FromBody] PresetDeviceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await _service.CreatePresetAsync(userDeviceId, dto))
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpDelete("{userDeviceId:int}/delete")]
    public async Task<IActionResult> DeleteUserDevice(int userDeviceId)
    {
        if (!await _service.DeleteUserDeviceAsync(userDeviceId))
        {
            return NotFound();
        }

        return NoContent();
    }
}
