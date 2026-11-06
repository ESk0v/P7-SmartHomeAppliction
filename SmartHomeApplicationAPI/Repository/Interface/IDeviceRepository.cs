using Utilities.Models;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IDeviceRepository
{
    Task<UserDevice?> FindUserDeviceByIdAsync(int deviceId);
    Task<DevicePreset?> FindPresetByIdAsync(int presetId);
    Task<Device?> FindDeviceByModelAndCategoryAsync(string model, string category);
    Task<DevicePreset?> FindPresetByDeviceIdAndPresetNameAsync(int deviceId, string presetName);
    Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId);
    Task<List<CatalogueDeviceDto>> GetAllDevicesAsync();
    Task AddDeviceAsync(Device device, DevicePreset preset, UserDevice userDevice);
    Task AddPresetAsync(DevicePreset preset);
    Task CommitChangesAsync();
    Task DeleteUserDevice(UserDevice userDevice);   
}