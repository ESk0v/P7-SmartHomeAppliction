using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class DeviceRepository : IDeviceRepository
{
    private readonly SmartHomeDbContext _db;

    public DeviceRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<UserDevice?> FindUserDeviceByIdAsync(int deviceId)
    {
        return await _db.UserDevice.FindAsync(deviceId);
    }

    public async Task<DevicePreset?> FindPresetByIdAsync(int presetId)
    {
        return await _db.Preset.FindAsync(presetId);
    }

    public async Task<Device?> FindDeviceByModelAndCategoryAsync(string model, string category)
    {
        return await _db.Device.FirstOrDefaultAsync(d => d.Model == model && d.Category == category);
    }

    public async Task<DevicePreset?> FindPresetByDeviceIdAndPresetNameAsync(int deviceId, string presetName)
    {
        return await _db.Preset.FirstOrDefaultAsync(p => p.DeviceId == deviceId && p.PresetName == presetName);
    }

    public async Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId)
    {
        return await _db.UserDevice
            .AsNoTracking()
            .Where(ud => ud.UserId == userId)
            .Select(ud => new UserDeviceDto
            {
                Id = ud.Id,
                Model = ud.Device!.Model,
                Category = ud.Device.Category,
                Nickname = ud.Nickname,
                Icon = ud.Icon,
                Color = ud.Color,
                IsFavorite = ud.IsFavorite,
                Presets = _db.Preset
                    .Where(preset => preset.DeviceId == ud.DeviceId)
                    .OrderBy(preset => preset.Id)
                    .Select(preset => new PresetDeviceDto
                    {
                        Id = preset.Id,
                        PresetName = preset.PresetName,
                        ConsumptionInKwh = preset.ConsumptionInKwh,
                        DurationInMinutes = preset.DurationInMinutes
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<List<CatalogueDeviceDto>> GetAllDevicesAsync()
    {
        return await _db.Device
            .AsNoTracking()
            .Select(device => new CatalogueDeviceDto
            {
                Model = device.Model,
                Category = device.Category,
                Presets = _db.Preset
                    .Where(preset => preset.DeviceId == device.Id)
                    .OrderBy(preset => preset.Id)
                    .Select(preset => new PresetDeviceDto
                    {
                        Id = preset.Id,
                        PresetName = preset.PresetName,
                        ConsumptionInKwh = preset.ConsumptionInKwh,
                        DurationInMinutes = preset.DurationInMinutes
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task AddDeviceAsync(Device device, DevicePreset preset, UserDevice userDevice)
    {
        if (device.Id == 0)
        {
            _db.Device.Add(device);
        }
        var existingPreset = _db.Preset.Where(p => p.DeviceId == device.Id && p.PresetName == preset.PresetName).FirstOrDefault();
        if (existingPreset == null)
        {
            _db.Preset.Add(preset);
        }
        _db.UserDevice.Add(userDevice);
        await _db.SaveChangesAsync();
    }

    public async Task AddPresetAsync(DevicePreset preset)
    {
        _db.Preset.Add(preset);
        await _db.SaveChangesAsync();
    }

    public async Task CommitChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    public async Task DeleteUserDevice(UserDevice userDevice)
    {
        _db.UserDevice.Remove(userDevice);
        await _db.SaveChangesAsync();
    }
}