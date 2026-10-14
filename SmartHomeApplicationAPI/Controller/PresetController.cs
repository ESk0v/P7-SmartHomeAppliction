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

    [HttpGet("get-preset")]
    public async Task<ActionResult<List<PresetDto>>> GetPresets()
    {
        return await _service.GetPresetsAsync();
    }
}