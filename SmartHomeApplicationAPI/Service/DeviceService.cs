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

        await _repository.CommitChangesAsync();
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

    public async Task<bool> UpdatePresetAsync(int userDeviceId, PresetDeviceDto preset)
    {
        var userDevice = await _repository.FindUserDeviceByIdAsync(userDeviceId);
        var existingPreset = await _repository.FindPresetByIdAsync(preset.Id);
        if (userDevice is null || existingPreset is null || existingPreset.DeviceId != userDevice.DeviceId)
        {
            return false;
        }

        existingPreset.ConsumptionInKwh = preset.ConsumptionInKwh;
        existingPreset.DurationInMinutes = preset.DurationInMinutes;
        await _repository.CommitChangesAsync();
        return true;
    }

    public async Task<bool> CreatePresetAsync(int userDeviceId, PresetDeviceDto preset)
    {
        var userDevice = await _repository.FindUserDeviceByIdAsync(userDeviceId);
        if (userDevice is null)
        {
            return false;
        }

        await _repository.AddPresetAsync(new DevicePreset
        {
            DeviceId = userDevice.DeviceId,
            PresetName = preset.PresetName,
            ConsumptionInKwh = preset.ConsumptionInKwh,
            DurationInMinutes = preset.DurationInMinutes
        });
        return true;
    }

    public async Task<int> CreateDeviceAsync(CreateDeviceDto dto)
    {   
      
        var device = await _repository.FindDeviceByModelAndCategoryAsync(dto.Model, dto.Category);
        if (device == null)
        {
            device = new Device
            {
                Model = dto.Model,
                Category = dto.Category
            };
        } 

        var preset = await _repository.FindPresetByDeviceIdAndPresetNameAsync(device.Id, dto.Preset);
        if (preset == null)
        {
            preset = new DevicePreset
            {
                Device = device,
                PresetName = dto.Preset,
                ConsumptionInKwh = dto.ConsumptionInKwh ?? 0m,
                DurationInMinutes = dto.DurationInMinutes ?? 0
            };
        }
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

    public async Task<bool> DeleteUserDeviceAsync(int userDeviceId)
    {
        var userDevice = await _repository.FindUserDeviceByIdAsync(userDeviceId);
        if (userDevice == null) return false;
        await _repository.DeleteUserDevice(userDevice);
        return true;
    }
}