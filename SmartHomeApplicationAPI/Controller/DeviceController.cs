using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var device = await _db.Device.FindAsync(id);
        if (device == null) return NotFound();
        return Ok(device);
    }

    [HttpPost("create-device")]
    public async Task<IActionResult> Create([FromBody] DevicesDto dto)
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
