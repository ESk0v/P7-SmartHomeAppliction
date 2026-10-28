// Repository/Co2ImportRepository.cs
using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Hangfire.Repository;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public class Co2ImportRepository
    : ICo2ImportRepository
{
    private readonly SmartHomeDbContext _db;

    public Co2ImportRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public Task<List<Electricity>> GetRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return _db.Electricity
            .AsTracking()
            .Where(item => item.Time >= from && item.Time < to)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}