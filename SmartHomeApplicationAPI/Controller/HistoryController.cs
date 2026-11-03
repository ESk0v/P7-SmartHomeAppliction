using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;

namespace Controller.History;

[ApiController]
[Route("api/history-controller")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryService _service;

    public HistoryController(IHistoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<HistoryPointDto>>> GetHistory(
        [FromHeader(Name = "UserId")] string userId,
        [FromQuery, BindRequired] DateTime from,
        [FromQuery, BindRequired] DateTime to)
    {
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (to <= from)
            return BadRequest("'to' must be after 'from'.");
            
        if ((to.Date - from.Date).TotalDays > 366)
            return BadRequest("Period cannot exceed one year.");

        return await _service.GetHistoryAsync(userId, from, to);
    }
}