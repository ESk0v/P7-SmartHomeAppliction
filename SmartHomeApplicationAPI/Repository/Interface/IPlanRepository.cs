using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IPlanRepository
{
    Task<List<PlanDto>> GetPlansAsync(string UserID, DateTime date);

    Task<PlanDTO?> GetPlanForEditAsync(string userId, int planId);

    Task<PlanUserDeviceDto?> GetUserDeviceForUpdateAsync(string userId, int userDeviceId);

    Task<bool> PresetBelongsToDeviceAsync(int presetId, int deviceId);

    Task<PlanDTO?> UpdatePlanAsync(string userId, int planId, PlanDTO update);
}
