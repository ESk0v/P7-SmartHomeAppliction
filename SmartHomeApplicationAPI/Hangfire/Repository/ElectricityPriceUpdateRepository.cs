using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public class ElectricityPriceUpdateRepository
    : IElectricityPriceUpdateRepository
{
    private readonly SmartHomeDbContext _db;

    public ElectricityPriceUpdateRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public Task<List<Electricity>> GetPricesAsync(
        DateTime from,
        DateTime to)
    {
        return _db.Electricity
            .Where(price => price.Time >= from && price.Time < to)
            .ToListAsync();
    }

    public async Task SavePricesAsync(
        IReadOnlyCollection<Electricity> newPrices)
    {
        _db.Electricity.AddRange(newPrices);

        await _db.SaveChangesAsync();
    }
}