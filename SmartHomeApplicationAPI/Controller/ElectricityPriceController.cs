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
}