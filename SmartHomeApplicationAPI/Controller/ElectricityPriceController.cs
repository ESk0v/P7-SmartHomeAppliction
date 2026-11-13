using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

namespace Controller;

[ApiController]
[Route("api/electricity-price-controller")]
public class ElectricityPriceController : ControllerBase
{
    private readonly IElectricityPriceService _service;

    public ElectricityPriceController(IElectricityPriceService service)
    {
        _service = service;
    }

    [HttpGet("dashboard-showcase")]
    public async Task<ActionResult<List<HourPriceDto>>> getElectricityPrices([FromQuery] string areaCode)
    {
        return await _service.GetPricesForDateAsync(areaCode);
    }

    [HttpPost("unscheduled-estimate")]
    public async Task<ActionResult<UnscheduledEstimateDto>> GetUnscheduledEstimate(
        [FromBody] UnscheduledEstimateRequestDto request)
    {
        if (request.EndTime <= request.StartTime || request.EnergyInKwh <= 0)
            return BadRequest("A positive workload and a valid time range are required.");

        UnscheduledEstimateResultDto result = await _service.GetUnscheduledEstimateAsync(request);
        return result.Estimate is null
            ? NotFound(result.Error)
            : Ok(result.Estimate);
    }
}