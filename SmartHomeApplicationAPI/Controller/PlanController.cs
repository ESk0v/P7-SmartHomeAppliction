using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

namespace Controller.Dashboard;
[ApiController]
[Route("api/plan-controller")]
public class PlanController : ControllerBase
{
    private readonly IPlanService _service;

    public PlanController(IPlanService service)
    {
        _service = service;
    }

    [HttpGet("get-plan")]
    public async Task<ActionResult<List<PlanDto>>> getPlans([FromHeader(Name = "UserId")] string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        return await _service.GetPlansAsync(userId);
    }
}