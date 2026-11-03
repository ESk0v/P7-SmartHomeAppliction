using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

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
    public async Task<ActionResult<StatisticsDto>> getPlannedCost([FromHeader(Name = "UserId")] string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        return await _service.GetPlannedCostAsync(userId);
    }

    [HttpGet("schedule-savings-cost")]
    public async Task<ActionResult<StatisticsDto>> GetScheduleSavingsCost([FromHeader(Name = "UserId")] string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

       return await _service.GetScheduleSavingCostAsync(userId);
    }

    [HttpGet("planned-co2")]
    public async Task<ActionResult<StatisticsDto>> getPlannedCo2([FromHeader(Name = "UserId")] string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        return await _service.GetPlannedCo2Async(userId);
    }

    [HttpGet("schedule-savings-c02")]
    public async Task<ActionResult<StatisticsDto>> GetScheduleSavingsCo2([FromHeader(Name = "UserId")] string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

       return await _service.GetScheduleSavingCo2Async(userId);
    }

    #endregion
    
    #region Optimized Statistics

    [HttpGet("estimated-cost")]
    public async Task<StatisticsDto> GetEstimatedCost()
    {
        return new StatisticsDto
        {
            Value = "6.15 kr",
            Description = "↓ 25% cheaper vs. starting now"
        };
    }

    [HttpGet("money-saved")]
    public async Task<StatisticsDto> GetMoneySaved()
    {
        return new StatisticsDto
        {
            Value = "4.25 kr",
            Description = "vs. unscheduled"
        };
    }

    [HttpGet("load-duration")]
    public async Task<StatisticsDto> GetLoadDuration()
    {
        return new StatisticsDto
        {
            Value = "3h 15m",
            Description = "starting at 22:00"
        };
    }

    [HttpGet("load-finish")]
    public async Task<StatisticsDto> GetLoadFinish()
    {
        return new StatisticsDto
        {
            Value = "01:15",
            Description = "4h 45m before deadline"
        };
    }
    
    #endregion
}