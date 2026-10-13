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
        return await _db.Plan
            .AsNoTracking()
            .OrderBy(s => s.StartTime)
            .Join(_db.UserDevices,
                s => s.UserDeviceId,
                d => d.Id,
                (s, d) => new PlanDto(d.Color, d.Nickname, s.StartTime, s.EndTime))
            .ToListAsync();
    }
}