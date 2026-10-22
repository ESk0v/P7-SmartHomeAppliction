using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface ICreatePlanService
{
    Task<List<PresetDto>> GetPresetsAsync(int deviceId);

    Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId);

    Task<PlanDTO> PostPlanAsync(PlanDTO planDTO);

}