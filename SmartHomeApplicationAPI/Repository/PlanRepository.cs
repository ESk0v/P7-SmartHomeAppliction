using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class PlanRepository : IPlanRepository
{
    private readonly SmartHomeDbContext _db;

    public PlanRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<List<PlanDto>> GetPlansAsync()
    {
        return await _db.Schedules
            .AsNoTracking()
            .Join(_db.Devices,
                s => s.DeviceId,
                d => d.Id,
                (s, d) => new PlanDto(d.Name, s.StartTime, s.EndTime))
            .ToListAsync();
    }
}