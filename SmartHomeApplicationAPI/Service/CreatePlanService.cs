using Utilities.Models;
using SmartHomeApplicationAPI.Repository;

namespace SmartHomeApplicationAPI.Service;

public class CreatePlanService : ICreatePlanService
{
    private readonly ICreatePlanRepository _repository;

    public CreatePlanService(ICreatePlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PresetDto>> GetPresetsAsync(int deviceId)
    {
        return await _repository.GetPresetsAsync(deviceId);
    }

    public async Task<List<PlanUserDeviceDto>> GetUserDevicesAsync(string userId)
    {
        return await _repository.GetUserDevicesAsync(userId);
    }

    public async Task<PlanDTO> PostPlanAsync(PlanDTO planDTO)
    {
        return await _repository.PostPlanAsync(planDTO);
    }

}