using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.Hangfire.Repository;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public class ElectricityPriceImportRepository
    : IElectricityPriceImportRepository
{
    private readonly SmartHomeDbContext _db;

    public ElectricityPriceImportRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public Task<List<Electricity>> GetRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return _db.Electricity
            .Where(item => item.Time >= from && item.Time < to)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(
        IReadOnlyCollection<Electricity> newRows,
        CancellationToken cancellationToken = default)
    {
        _db.Electricity.AddRange(newRows);
        await _db.SaveChangesAsync(cancellationToken);
    }
}