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

}