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

    [HttpGet("planned-cost")]
    public async Task<ActionResult<StatisticsDto>> getPlannedCost([FromHeader(Name = "UserId")] string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        return await _service.GetPlannedCostAsync(userId);
    }

    [HttpGet("cheapest-period")]
    public async Task<StatisticsDto> GetCheapestPeriod()
    {
        return new StatisticsDto
        {
            Value = "01-06",
            Description = " avg 17 øre/kWh"
        };
    }

    [HttpGet("schedule-savings")]
    public async Task<StatisticsDto> GetScheduleSavings()
    {
        return new StatisticsDto
        {
            Value = "6.15 kr",
            Description = "25% cheaper"
        };
    }

    [HttpGet("peak-shift")]
    public async Task<StatisticsDto> GetPeakShift()
    {
        return new StatisticsDto
        {
            Value = "14.8 kWh",
            Description = "Moved to offpeak"
        };
    }
}