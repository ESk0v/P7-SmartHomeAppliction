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

    Task UpdateUserDeviceAsync(
        UpdateUserDeviceDto UpdateUserDeviceDto,
        CancellationToken cancellationToken = default);

    Task UpdatePresetAsync(
        int userDeviceId,
        PresetDeviceDto preset,
        CancellationToken cancellationToken = default);

    Task CreatePresetAsync(
        int userDeviceId,
        PresetDeviceDto preset,
        CancellationToken cancellationToken = default);

    Task DeleteUserDeviceAsync(
        int userDeviceId,
        CancellationToken cancellationToken = default);

}
