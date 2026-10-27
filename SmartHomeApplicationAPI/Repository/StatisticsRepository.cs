using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class StatisticsRepository : IStatisticsRepository
{
    private readonly SmartHomeDbContext _db;

    public StatisticsRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<List<Plan>> RetrievePlansAsync(string userId, DateTime start, DateTime end)
    {
        return await _db.Plan
            .Where(p => p.UserDevice.UserId == userId && p.EndTime <= end && p.StartTime >= start) 
            .AsNoTracking()
            .ToListAsync();   
    }
}