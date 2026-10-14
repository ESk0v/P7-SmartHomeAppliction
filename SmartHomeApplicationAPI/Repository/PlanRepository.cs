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

    public async Task<List<PlanDto>> GetPlansAsync(string userId, DateTime date)
    {
        return await _db.Plan
            .Where(p => p.UserDevice.UserId == userId && p.EndTime >= date)
            .AsNoTracking()
            .OrderBy(p => p.StartTime)
            .Select(p => new PlanDto(
                icon: p.UserDevice.Icon,
                colorCode: p.UserDevice.Color,
                deviceName: p.UserDevice.Nickname,
                startTime: p.StartTime,
                endTime: p.EndTime
            ))
            .ToListAsync();
    }
}