using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class ElectricityPriceRepository : IElectricityPriceRepository
{
    private readonly SmartHomeDbContext _db;

    public ElectricityPriceRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<List<HourPriceDto>> GetPricesForDateAsync(DateTime date)
    {
        return await _db.DayAheadPrices
            .Where(p => p.Time >= date)
            .OrderBy(p => p.Time)
            .Select(p => new HourPriceDto(p.Time, p.Price))
            .ToListAsync();
    }
}