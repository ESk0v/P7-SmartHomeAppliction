using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class ForecastingRepository : IForecastingRepository
{
    private readonly SmartHomeDbContext _db;

    public ForecastingRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    // Method implementation here
    public async Task<List<HistoricalPricePoint>> GetRecentPricesAsync(string priceArea, int count = 96)
    {
        return await _db.Electricity
            .Where(e => e.PriceArea == priceArea && !e.IsPredicted)
            .OrderByDescending(e => e.Time)
            .Take(count)
            .OrderBy(e => e.Time)
            .Select(e => new HistoricalPricePoint
            {
                Time = e.Time,
                PricePerKwh = (float)e.PricePerKwh
            })
            .ToListAsync();
    }

    public async Task<ForecastingResponseDto> PostPrices(ForecastingResponseDto respDto)
    {
        // All database communication should reside here

        // Tag imod ResponseDto, pak listen ad Predictions ud og tilføj den til database, er jeg ret sikker på
            // Mapping gøres med Select() hvor hver PredictedPricePoint fra respDto pakke ud til 1 Electrity entry
        var prices = respDto.Predictions.Select(p => new Electricity
        {
            Time = p.Time,
            PricePerKwh = (decimal)p.PricePerKwh,
            PriceArea = p.PriceArea,
            IsPredicted = p.IsPredicted
        }).ToList();

        await _db.Electricity.AddRangeAsync(prices);
        await _db.SaveChangesAsync();

        return respDto;
    }
}
