using FluentValidation;
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

    [HttpGet("get-plan/{planId:int}")]
    public async Task<ActionResult<PlanDTO>> GetPlanForEdit(
        int planId, [FromHeader(Name = "UserId")] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        PlanDTO? plan = await _service.GetPlanForEditAsync(userId, planId);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPut("update-plan/{planId:int}")]
    public async Task<ActionResult<PlanDTO>> UpdatePlan(
        int planId, [FromHeader(Name = "UserId")] string userId, [FromBody] PlanDTO update)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (update.planId.HasValue && update.planId.Value != planId)
            return BadRequest("The plan ID in the body does not match the route.");

        try
        {
            PlanDTO? saved = await _service.UpdatePlanAsync(userId, planId, update);
            return saved is null ? NotFound() : Ok(saved);
        }
        catch (ValidationException exception)
        {
            return BadRequest(string.Join(" ", exception.Errors.Select(error => error.ErrorMessage)));
        }
    }
}
