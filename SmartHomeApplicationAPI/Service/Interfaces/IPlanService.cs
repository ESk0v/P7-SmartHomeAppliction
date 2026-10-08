using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface IPlanService
{
    Task<List<PlanDto>> GetPlansAsync();
}