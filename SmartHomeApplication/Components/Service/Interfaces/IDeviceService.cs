using Utilities.Models;

namespace SmartHomeApplication.Service;

public interface IDeviceService
{
    Task CreateDeviceAsync(
        DevicesDto deviceDto,
        CancellationToken cancellationToken = default);
}
