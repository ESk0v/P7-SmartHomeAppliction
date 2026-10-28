using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace SmartHomeApplicationAPI.Controllers;

[ApiController]
[Route("api/device-controller")]
public class DeviceController : ControllerBase
{
    private readonly SmartHomeDbContext _db;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(SmartHomeDbContext db, ILogger<DeviceController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPut("update-userdevice")]
    public async Task<IActionResult> UpdateDevice([FromBody] UpdateUserDeviceDto dto)
    {
        _logger.LogWarning("Received request to update device: {@UpdateUserDeviceDto}", dto);
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState invalid: {@ModelState}", ModelState);
            return ValidationProblem(ModelState);
        }
        _logger.LogWarning("Attempting to find device with with id {DeviceId} and nickname {Nickname}", dto.Id, dto.Nickname);

        var userDevice = await _db.UserDevice.FindAsync(dto.Id);
        if (userDevice == null)
        {
            _logger.LogWarning("Device not found: {DeviceId}", dto.Id);
            return NotFound();
        }

        userDevice.Nickname = dto.Nickname;
        userDevice.Icon = dto.Icon;
        userDevice.Color = dto.Color;
        userDevice.IsFavorite = dto.IsFavorite;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Device updated successfully with id={DeviceId}", dto.Id);
        return Ok(new { message = "Device updated successfully", id = dto.Id });
    }

    [HttpGet("get-userdevices")]
    public async Task<IActionResult> GetUserDevices([FromHeader(Name = "User-Id")] string userId, CancellationToken ct)
    {
        _logger.LogInformation("Received request to get devices for userId={UserId}", userId);
        var userDevices = await _db.UserDevice
            .Where(ud => ud.UserId == userId)
            .Select(ud => new UserDeviceDto
            {
                Id = ud.Id,
                Model = ud.Device.Model,
                Category = ud.Device.Category,
                Nickname = ud.Nickname,
                Icon = ud.Icon,
                Color = ud.Color,
                IsFavorite = ud.IsFavorite
            })
            .ToListAsync(ct);
        _logger.LogInformation("Retrieved {DeviceCount} devices for userId={UserId}", userDevices.Count, userId);
        return Ok(userDevices);
    }

    [HttpGet("get-all-devices")]
    public async Task<IActionResult> GetAllDevices(CancellationToken ct)
    {
        _logger.LogInformation("Received request to get all devices");

        var devices = await _db.Device
            .Select(d => new CatalogueDeviceDto
            {
                Model = d.Model,
                Category = d.Category
            })
            .ToListAsync(ct);

        _logger.LogInformation("Retrieved {DeviceCount} devices", devices.Count);
        return Ok(devices);
    }

    [HttpPost("create-device")]
    public async Task<IActionResult> Create([FromBody] CreateDeviceDto dto)
    {
        _logger.LogInformation("Received request to create device: {@DevicesDto}", dto);
        if (!ModelState.IsValid) { 
            _logger.LogWarning("ModelState invalid: {@ModelState}", ModelState);
            return ValidationProblem(ModelState);
        }

        var device = new Device
        {
            Model = dto.Model,
            Category = dto.Category
        };

        var devicePreset = new DevicePreset
        {
            Device = device,
            PresetName = dto.Preset,
            ConsumptionInKwh = dto.ConsumptionInKwh ?? 0m,
            DurationInMinutes = dto.DurationInMinutes ?? 0
        };

        var userDevice = new UserDevice
        {
            Device = device,
            UserId = dto.UserId,
            Nickname = dto.Model,
            Icon = "default_icon",
            Color = "#ea580b",
            IsFavorite = false
        };

        _db.Device.Add(device);
        _db.Preset.Add(devicePreset);
        _db.UserDevice.Add(userDevice);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Device created successfully with id={DeviceId}", device.Id);
        return Ok(new { message = "Device created successfully", id = device.Id });
    }
}
