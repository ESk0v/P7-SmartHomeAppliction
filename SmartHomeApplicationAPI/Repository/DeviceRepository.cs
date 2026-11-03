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
                Preset = _db.Preset
                    .Where(preset => preset.DeviceId == device.Id)
                    .OrderBy(preset => preset.Id)
                    .Select(preset => preset.PresetName)
                    .FirstOrDefault() ?? "standard",
                ConsumptionInKwh = _db.Preset
                    .Where(preset => preset.DeviceId == device.Id)
                    .OrderBy(preset => preset.Id)
                    .Select(preset => (decimal?)preset.ConsumptionInKwh)
                    .FirstOrDefault(),
                DurationInMinutes = _db.Preset
                    .Where(preset => preset.DeviceId == device.Id)
                    .OrderBy(preset => preset.Id)
                    .Select(preset => (int?)preset.DurationInMinutes)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task AddDeviceAsync(Device device, DevicePreset preset, UserDevice userDevice)
    {
        _db.Device.Add(device);
        _db.Preset.Add(preset);
        _db.UserDevice.Add(userDevice);
        await _db.SaveChangesAsync();
    }

    public async Task AddPresetAsync(DevicePreset preset)
    {
        _db.Preset.Add(preset);
        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    public async Task DeleteUserDevice(UserDevice userDevice)
    {
        _db.UserDevice.Remove(userDevice);
        await _db.SaveChangesAsync();
    }
}