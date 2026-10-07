using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;

namespace Controller.Dashboard;
[ApiController]
[Route("api/statistics-controller")]
public class StatisticsController : ControllerBase
{
    public StatisticsController()
    {

    }

    [HttpGet("planned-cost")]
    public async Task<StatisticsDto> getPlannedCost()
    {
        return new StatisticsDto
        {
            Value = "18.40 kr"
        };
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