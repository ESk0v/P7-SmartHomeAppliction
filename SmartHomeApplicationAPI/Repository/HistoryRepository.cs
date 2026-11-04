using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;

namespace SmartHomeApplicationAPI.Repository;

public class HistoryRepository : IHistoryRepository
{
    private readonly SmartHomeDbContext _context;

    public HistoryRepository(SmartHomeDbContext context)
    {
        _context = context;
    }

    public Task<List<PlanSummary>> RetrievePlanSummariesAsync(string userId, DateTime start, DateTime end) =>
        _context.Plan
            .AsNoTracking()
            .Where(p => p.UserDevice.UserId == userId
                     && p.PlanEndTime <= end
                     && p.PlanStartTime >= start)
            .Select(p => new PlanSummary(
                p.PlanStartTime,
                p.EstimatedPrice,
                p.EstimatedCo2Emissions))
            .ToListAsync();
}