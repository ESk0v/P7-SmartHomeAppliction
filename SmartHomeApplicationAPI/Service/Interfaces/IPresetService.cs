using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface IPresetService
{
    Task<List<PresetDto>> GetPresetsAsync(int deviceId);
}