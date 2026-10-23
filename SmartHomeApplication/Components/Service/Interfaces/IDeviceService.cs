using Utilities.Models;

namespace SmartHomeApplication.Service;

public interface IDeviceService
{
    Task CreateDeviceAsync(
        CreateDeviceDto deviceDto,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<CatalogueDeviceDto>> GetAllDevicesAsync(
        CancellationToken cancellationToken = default);

    Task<IEnumerable<UserDeviceDto>> GetUserDevicesAsync(
        CancellationToken cancellationToken = default);
}
