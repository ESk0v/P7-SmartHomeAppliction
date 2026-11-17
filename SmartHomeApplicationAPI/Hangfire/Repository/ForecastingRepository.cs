using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public class ForecastingRepository : IForecastingRepository
{
    private readonly SmartHomeDbContext _db;

    public ForecastingRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    // Method implementation here
    public async Task<DateTime?> GetLatestActualPriceTimeAsync(string priceArea)
    {
        return await _db.Electricity
            .Where(e => e.PriceArea == priceArea && !e.IsPredicted)
            .MaxAsync(e => (DateTime?)e.Time);
    }

    public async Task<ForecastingResponseDto> PostPrices(ForecastingResponseDto respDto)
    {
        if (respDto.Predictions.Count == 0)
            return respDto;

        var targetDate = respDto.Predictions.First().Time.Date;

        // Get existing timestamps for the target date and area
        var existingTimes = await _db.Electricity
            .AsNoTracking()
            .Where(e =>
                e.PriceArea == respDto.PriceArea &&
                e.Time >= targetDate &&
                e.Time < targetDate.AddDays(1))
            .Select(e => e.Time)
            .ToListAsync();

        var existingTimeSet = existingTimes.ToHashSet();

        // Insert only missing predictions
        var prices = respDto.Predictions
            .Where(p => !existingTimeSet.Contains(p.Time))
            .Select(p => new Electricity
            {
                Time = DateTime.SpecifyKind(p.Time, DateTimeKind.Unspecified),
                PricePerKwh = Math.Round((decimal)p.PricePerKwh, 5),
                PriceArea = p.PriceArea.ToUpperInvariant(),
                IsPredicted = true
            })
            .ToList();

        if (prices.Count > 0)
        {
            await _db.Electricity.AddRangeAsync(prices);
            await _db.SaveChangesAsync();
        }

        return respDto;
    }
}
