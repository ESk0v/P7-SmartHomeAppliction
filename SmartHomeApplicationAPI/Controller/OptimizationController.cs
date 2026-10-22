using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;

namespace Controller.Dashboard;

[ApiController]
[Route("api/optimization-controller")]
public class OptimizationController : ControllerBase
{
    private readonly IOptimizationService _service;

    public OptimizationController(IOptimizationService service)
    {
        _service = service;
    }

    [HttpPost("optimize")]
    public async Task<ActionResult<OptimizationResultDTO>> Optimize([FromBody] OptimizationRequestDTO request)
    {
        var result = await _service.OptimizeAsync(request);

        return Ok(result);
    }
}    