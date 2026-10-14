using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

namespace Controller.Dashboard;
[ApiController]
[Route("api/preset-controller")]
public class PresetController : ControllerBase
{
    private readonly IPresetService _service;

    public PresetController(IPresetService service)
    {
        _service = service;
    }

    [HttpGet("get-preset/{deviceId:int}")]
    public async Task<ActionResult<List<PresetDto>>> GetPresets(int deviceId)
    {
        return await _service.GetPresetsAsync(deviceId);
    }
}