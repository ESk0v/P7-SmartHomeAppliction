using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class PresetRepository : IPresetRepository
{
    private readonly SmartHomeDbContext _db;

    public PresetRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<List<PresetDto>> GetPresetsAsync(int deviceId)
    {
        return await _db.Preset
            .Where(p => p.DeviceId == deviceId)
            .Select(p => new PresetDto(
                p.Id,
                p.PresetName,
                p.ConsumptionInKwh,
                p.DurationInMinutes
            ))
            .ToListAsync();
        
    }

    public async Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId)
    {
        return await _db.UserDevice
            .AsNoTracking()
            .Where(ud => ud.UserId == userId)
            .Join(_db.Device,
                ud => ud.DeviceId,
                d => d.Id,
                (ud, d) => new UserDeviceDto(
                    ud.Id,
                    ud.Nickname,
                    d.Category,
                    d.Model
                ))
            .ToListAsync();
    }
    
}