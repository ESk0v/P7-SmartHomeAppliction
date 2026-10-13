using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Controllers;

[ApiController]
[Route("api/device-controller")]
public class DeviceController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public DeviceController(SmartHomeDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var device = await _db.Devices.FindAsync(id);
        if (device == null) return NotFound();
        return Ok(device);
    }

    [HttpPost("create-device")]
    public async Task<IActionResult> Create([FromBody] DevicesDTO dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var device = new Devices
        {
            Name = dto.Name,
            Category = dto.Category,
            Preset = dto.Preset,
            ConsumptionInKwh = dto.ConsumptionInKwh ?? 0m,
            DurationInMinutes = dto.DurationInMinutes ?? 0
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Device created successfully", id = device.Id });
    }
}
