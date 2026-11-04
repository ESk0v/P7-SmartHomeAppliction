using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface IPlanService
{
    Task<List<PlanDto>> GetPlansAsync(string userID);

    Task<PlanDTO?> GetPlanForEditAsync(string userId, int planId);

    Task<PlanDTO?> UpdatePlanAsync(string userId, int planId, PlanDTO update);
}
