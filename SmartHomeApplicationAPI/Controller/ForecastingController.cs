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
        // DER ER POTENTIELT NOGET GALT MED METHODS HELE VEJEN IGENNEM DETTE FLOW
        // OG DERES PARAMETRE OG RETURN TYPER. Tror jeg er kommet til at rodde rundt i Request og Response.
        // Skal der tjekkes op på
        return null;
    }
}    