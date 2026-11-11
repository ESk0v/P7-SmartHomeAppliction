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
    public async Task<ActionResult<ForecastingResponseDto>> Forecast(string priceArea = "DK1", [FromQuery] int stepsAhead = 96)
    {
        // Mangler lige lidt fejl tjeks, men ellers. 

        // Er KUN brugt til test i Swagger, så lige nu bruges den ikke. Beholer hvis vi skal teste mere i Swagger
        var result = await _service.GenerateForecastForAreaAsync(priceArea, stepsAhead);
        return Ok(result);
    }
}    