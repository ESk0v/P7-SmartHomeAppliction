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
}