using Utilities.Models;
using SmartHomeApplicationAPI.Repository;

namespace SmartHomeApplicationAPI.Service;

public class PresetService : IPresetService
{
    private readonly IPresetRepository _repository;

    public PresetService(IPresetRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PresetDto>> GetPresetsAsync(int deviceId)
    {
        return await _repository.GetPresetsAsync(deviceId);
    }

    public async Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId)
    {
        return await _repository.GetUserDevicesAsync(userId);
    }

    public async Task<PlanDTO> PostPlanAsync(PlanDTO planDTO)
    {
        return await _repository.PostPlanAsync(planDTO);
    }

}