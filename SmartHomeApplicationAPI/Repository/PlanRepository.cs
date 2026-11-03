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
            .Where(p => p.UserDevice.UserId == userId && p.PlanEndTime >= date)
            .AsNoTracking()
            .OrderBy(p => p.PlanStartTime)
            .Select(p => new PlanDto(
                planId: p.Id,
                nextPlanId: p.NextPlanId,
                planName: p.Name,
                icon: p.UserDevice.Icon,
                colorCode: p.UserDevice.Color,
                deviceName: p.UserDevice.Nickname,
                startTime: p.PlanStartTime,
                endTime: p.PlanEndTime,
                estimatedCost: p.EstimatedPrice,
                estimatedCo2Emissions: p.EstimatedCo2Emissions
                
            ))
            .ToListAsync();
    }
}
