using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

namespace Controller.Dashboard;
[ApiController]
[Route("api/createPlan-controller")]
public class CreatePlanController : ControllerBase
{
    private readonly ICreatePlanService _service;
    private readonly ILogger<CreatePlanController> _logger;

    public CreatePlanController(ICreatePlanService service, ILogger<CreatePlanController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("get-preset/{deviceId:int}")]
    public async Task<ActionResult<List<PresetDto>>> GetPresets(int deviceId)
    {
        if (deviceId <= 0)
        {
            return BadRequest("Invalid device ID.");
        }
        try
        {
            var presets = await _service.GetPresetsAsync(deviceId);
            return Ok(presets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving presets for device {DeviceId}", deviceId);

            return StatusCode(500, "An error occurred while retrieving presets.");
        }
    }

    [HttpGet("get-user-device/{userId}")]
    public async Task<ActionResult<List<PlanUserDeviceDto>>> GetUserDevices(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("Invalid or missing User ID.");
        }

        try
        {
            var devices = await _service.GetUserDevicesAsync(userId);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices for user {UserId}", userId);

            return StatusCode(500, "An error occurred while retrieving user devices.");
        }
    }

    [HttpPost("post-plan")]
    public async Task<ActionResult> PostPlan([FromBody] PlanDTO planDTO)
    {
        if (planDTO == null)
        {
            return BadRequest("Plan is required.");
        }

        try
        {
            var savedPlan = await _service.PostPlanAsync(planDTO);

            return Ok(savedPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan");

            return StatusCode(500, "An error occurred while creating the plan.");
        }
    }

    [HttpPost("optimize")]
    public async Task<ActionResult<OptimizationResultDTO>> Optimize(
        [FromBody] OptimizationRequestDTO request)
    {
        var result = await _service.OptimizeAsync(request);
        return Ok(result);
    }
}