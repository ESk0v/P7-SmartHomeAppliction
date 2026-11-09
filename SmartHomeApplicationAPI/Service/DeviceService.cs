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

        if (userDevice.Device?.Category == "Interruptible")
        {
            if (device.BatteryCapacityInKwh is null || device.ConsumptionInKwh is null)
                throw new ArgumentException("Battery capacity and consumption are required for interruptible devices.");

            DevicePreset? preset = await _repository.FindPresetForDeviceAsync(userDevice.DeviceId);
            if (preset is null) return false;
            preset.BatteryCapacityInKwh = device.BatteryCapacityInKwh;
            preset.ConsumptionInKwh = device.ConsumptionInKwh.Value;
        }

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
        if (userDevice is null || userDevice.Device?.Category == "Interruptible" ||
            existingPreset is null || existingPreset.DeviceId != userDevice.DeviceId)
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
        if (userDevice is null || userDevice.Device?.Category == "Interruptible")
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

        DevicePreset? preset = null;
        if (dto.Category == "Interruptible" && device.Id != 0)
        {
            DevicePreset? existingPreset = await _repository.FindPresetForDeviceAsync(device.Id);
            if (existingPreset is not null)
            {
                if (existingPreset.BatteryCapacityInKwh is not null &&
                    (existingPreset.BatteryCapacityInKwh != dto.BatteryCapacityInKwh ||
                     existingPreset.ConsumptionInKwh != dto.ConsumptionInKwh))
                    throw new DeviceCapacityConflictException();

                existingPreset.BatteryCapacityInKwh = dto.BatteryCapacityInKwh;
                existingPreset.ConsumptionInKwh = dto.ConsumptionInKwh!.Value;
            }
            else
            {
                preset = CreatePreset(device, dto);
            }
        }
        else
        {
            preset = CreatePreset(device, dto);
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

    private static DevicePreset CreatePreset(Device device, CreateDeviceDto dto) => new()
    {
        Device = device,
        PresetName = dto.Category == "Interruptible" ? "Info" : dto.Preset,
        ConsumptionInKwh = dto.ConsumptionInKwh!.Value,
        DurationInMinutes = dto.Category == "Interruptible" ? null : dto.DurationInMinutes,
        BatteryCapacityInKwh = dto.Category == "Interruptible" ? dto.BatteryCapacityInKwh : null
    };

    public async Task<bool> DeleteUserDeviceAsync(int userDeviceId)
    {
        var userDevice = await _repository.FindUserDeviceByIdAsync(userDeviceId);
        if (userDevice == null) return false;
        await _repository.DeleteUserDevice(userDevice);
        return true;
    }
}

public sealed class DeviceCapacityConflictException : Exception
{
    public DeviceCapacityConflictException()
        : base("This model already has different battery capacity or consumption values. Select its saved values from the catalogue or edit them in the device registry.")
    {
    }
}
