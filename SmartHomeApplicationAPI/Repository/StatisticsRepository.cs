using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using Utilities.Models;
19 january
namespace SmartHomeApplicationAPI.Repository;

public class StatisticsRepository : IStatisticsRepository
{
    private readonly SmartHomeDbContext _db;

    public StatisticsRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<StatisticsDto> GetPlannedCostAsync(string userId, Date start, Date end)
    {
        return await _db.Plan
            
    }
}