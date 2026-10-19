using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IPresetRepository
{
    Task<List<PresetDto>> GetPresetsAsync(int deviceId);

    Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId);
}