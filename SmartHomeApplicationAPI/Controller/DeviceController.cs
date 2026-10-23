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

    [HttpGet("get-userdevices")]
    public async Task<IActionResult> GetUserDevices([FromHeader(Name = "User-Id")] string userId, CancellationToken ct)
    {
        _logger.LogInformation("Received request to get devices for userId={UserId}", userId);
        var userDevices = await _db.UserDevice
            .Where(ud => ud.UserId == userId)
            .Select(ud => new UserDeviceDto
            {
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
