using Utilities.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Service;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _repository;

    public DeviceService(IDeviceRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> UpdateUserDeviceAsync(UpdateUserDeviceDto device)
    {
        var userDevice = await _repository.FindUserDeviceByIdAsync(device.Id);
        if (userDevice == null) return false;

        userDevice.Nickname = device.Nickname;
        userDevice.Icon = device.Icon;
        userDevice.Color = device.Color;
        userDevice.IsFavorite = device.IsFavorite;

        await _repository.SaveChangesAsync();
        return true;
    }

    public Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId)
    {
        return _repository.GetUserDevicesAsync(userId);
    }

    public Task<List<CatalogueDeviceDto>> GetAllDevicesAsync()
    {
        return _repository.GetAllDevicesAsync();
    }

    public async Task<int> CreateDeviceAsync(CreateDeviceDto dto)
    {
        var device = new Device
        {
            Model = dto.Model,
            Category = dto.Category
        };

        var preset = new DevicePreset
        {
            Device = device,
            PresetName = dto.Preset,
            ConsumptionInKwh = dto.ConsumptionInKwh ?? 0m,
            DurationInMinutes = dto.DurationInMinutes ?? 0
        };

        var userDevice = new UserDevice
        {
            Device = device,
            UserId = dto.UserId,
            Nickname = dto.Model,
            Icon = "default_icon",
            Color = "#ea580b",
            IsFavorite = false
        };

        await _repository.AddDeviceAsync(device, preset, userDevice);
        return device.Id;
    }
}