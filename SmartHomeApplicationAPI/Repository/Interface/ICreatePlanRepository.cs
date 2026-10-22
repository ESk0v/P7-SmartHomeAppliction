using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface ICreatePlanRepository
{
    Task<List<PresetDto>> GetPresetsAsync(int deviceId);

    Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId);

    Task<PlanDTO> PostPlanAsync(PlanDTO planDTO);

}