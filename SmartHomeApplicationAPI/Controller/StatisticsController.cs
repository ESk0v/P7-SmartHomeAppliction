using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Controller.Dashboard;

[ApiController]
[Route("api/statistics-controller")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _service;
    public StatisticsController(IStatisticsService service)
    {
        _service = service;
    }

    #region Dashboard Statistics

    [HttpGet("planned-cost")]
    public async Task<ActionResult<StatisticsDto>> getPlannedCost(
        [FromHeader(Name = "UserId")] string userId,
        [FromQuery, BindRequired] DateTime from,
        [FromQuery, BindRequired] DateTime to)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        if (to <= from)
            return BadRequest("'to' must be after 'from'.");

        return await _service.GetPlannedCostAsync(userId, from, to);
    }

    [HttpGet("schedule-savings-cost")]
    public async Task<ActionResult<StatisticsDto>> GetScheduleSavingsCost(
        [FromHeader(Name = "UserId")] string userId,
        [FromQuery, BindRequired] DateTime from,
        [FromQuery, BindRequired] DateTime to)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        if (to <= from)
            return BadRequest("'to' must be after 'from'.");

        return await _service.GetScheduleSavingCostAsync(userId, from, to);
    }

    [HttpGet("planned-co2")]
    public async Task<ActionResult<StatisticsDto>> getPlannedCo2(
        [FromHeader(Name = "UserId")] string userId,
        [FromQuery, BindRequired] DateTime from,
        [FromQuery, BindRequired] DateTime to)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        if (to <= from)
            return BadRequest("'to' must be after 'from'.");

        return await _service.GetPlannedCo2Async(userId, from, to);
    }

    [HttpGet("schedule-savings-c02")]
    public async Task<ActionResult<StatisticsDto>> GetScheduleSavingsCo2(
        [FromHeader(Name = "UserId")] string userId,
        [FromQuery, BindRequired] DateTime from,
        [FromQuery, BindRequired] DateTime to)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        if (to <= from)
            return BadRequest("'to' must be after 'from'.");

        return await _service.GetScheduleSavingCo2Async(userId, from, to);
    }

    #endregion
    
    #region Optimized Statistics

    [HttpGet("estimated-cost")]
    public async Task<ActionResult<StatisticsDto>> GetEstimatedCost([FromHeader(Name = "UserId")] string userId, [FromHeader(Name = "PlanId")] int planId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (planId <= 0)
            return BadRequest("PlanId header is required.");

        return await _service.GetEstimatedCostAsync(userId, planId);
    }

    [HttpGet("money-saved")]
    public async Task<ActionResult<StatisticsDto>> GetMoneySaved([FromHeader(Name = "UserId")] string userId, [FromHeader(Name = "PlanId")] int planId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (planId <= 0)
            return BadRequest("PlanId header is required.");

        return await _service.GetMoneySavedAsync(userId, planId);
    }

    [HttpGet("load-duration")]
    public async Task<ActionResult<StatisticsDto>> GetLoadDuration([FromHeader(Name = "UserId")] string userId, [FromHeader(Name = "PlanId")] int planId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (planId <= 0)
            return BadRequest("PlanId header is required.");

        return await _service.GetLoadDurationAsync(userId, planId);
    }

    [HttpGet("load-finish")]
    public async Task<ActionResult<StatisticsDto>> GetLoadFinish([FromHeader(Name = "UserId")] string userId, [FromHeader(Name = "PlanId")] int planId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (planId <= 0)
            return BadRequest("PlanId header is required.");

        return await _service.GetLoadFinishAsync(userId, planId);
    }
    
    #endregion
}
