using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;

namespace SmartHomeApplicationAPI.Controllers.Forecast;

[ApiController]
[Route("api")]
public class ForecastingController : ControllerBase
{
    private readonly IForecastingService _service;

    public ForecastingController(IForecastingService service)
    {
        _service = service;
    }

    [HttpPost("forecast")]
    public async Task<ActionResult<ForecastingResponseDto>> Forecast([FromBody] ForecastingRequestDto request)
    {
        return null;
    }
}    