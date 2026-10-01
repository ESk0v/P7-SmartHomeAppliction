using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public class ElectricityPriceImportRepository
    : IElectricityPriceImportRepository
{
    private readonly SmartHomeDbContext _db;

    public ElectricityPriceImportRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task UpdateInsertAsync(
        IReadOnlyCollection<DayAheadPrice> prices,
        CancellationToken cancellationToken = default)
    {
        foreach (var price in prices)
        {
            var existing = await _db.DayAheadPrices
                .SingleOrDefaultAsync(
                    item =>
                        item.Time == price.Time &&
                        item.PriceArea == price.PriceArea,
                    cancellationToken);

            if (existing is null)
            {
                _db.DayAheadPrices.Add(price);
            }
            else
            {
                existing.Price = price.Price;
                existing.IsPredicted = price.IsPredicted;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var nextDate = date.Date.AddDays(1);

        return _db.DayAheadPrices.CountAsync(
            item =>
                item.Time >= date.Date &&
                item.Time < nextDate,
            cancellationToken);
    }
}