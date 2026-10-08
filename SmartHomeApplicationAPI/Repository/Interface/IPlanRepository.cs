using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IPlanRepository
{
    Task<List<PlanDto>> GetPlansAsync(DateTime date);
}