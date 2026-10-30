using Utilities.Models;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IDeviceRepository
{
    Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId);
    Task<List<CatalogueDeviceDto>> GetAllDevicesAsync();
    Task AddDeviceAsync(Device device, DevicePreset preset, UserDevice userDevice);
    Task<bool> UpdateUserDeviceAsync(UserDevice userDevice);
}