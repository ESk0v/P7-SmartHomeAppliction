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

    public async Task<List<HourPriceDto>> GetPricesForDateAsync(string areaCode, DateTime date)
    {
        return await _db.Electricity
            .Where(p => p.Time >= date && p.PriceArea == areaCode)
            .OrderBy(p => p.Time)
            .Select(p => new HourPriceDto(p.Time, p.PricePerKwh))
            .ToListAsync();
    }

    public async Task<List<ElectricityReadingDto>> GetReadingsAsync(
        string areaCode,
        DateTime startTime,
        DateTime endTime)
    {
        return await _db.Electricity
            .Where(p => p.PriceArea == areaCode
                && p.Time >= startTime
                && p.Time < endTime)
            .Select(p => new ElectricityReadingDto(
                p.Time,
                p.PricePerKwh,
                p.Co2EmissionGramsPerKwh))
            .ToListAsync();
    }
}