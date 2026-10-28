using Utilities.Models;

namespace SmartHomeApplicationAPI.Service
{
    public interface IDeviceService
    {
        Task<bool> UpdateUserDeviceAsync(UpdateUserDeviceDto device);
        Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId);
        Task<List<CatalogueDeviceDto>> GetAllDevicesAsync();
        Task<int> CreateDeviceAsync(CreateDeviceDto device);
    }
}
