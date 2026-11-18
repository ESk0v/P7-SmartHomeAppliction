using Utilities.Models;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.DTOs.Optimization;

namespace SmartHomeApplicationAPI.Repository;

public interface ICreatePlanRepository
{
    Task<List<PresetDto>> GetPresetsAsync(int deviceId);

    Task<List<PlanUserDeviceDto>> GetUserDevicesAsync(string userId);

    Task<PlanDTO> PostPlanAsync(PlanDTO planDTO);

#region Optimization
    Task<List<EnergySlot>> GetEnergySlotsAsync(string area, DateTime from, DateTime to);
    Task<List<(PlanUserDeviceDto Device, DevicePreset Preset)>> GetDevicesWithPresetsAsync(int userDeviceId);
    Task<List<Plan>> GetActivePlansAsync(IReadOnlyCollection<int> userDeviceIds, DateTime from);

#endregion

}