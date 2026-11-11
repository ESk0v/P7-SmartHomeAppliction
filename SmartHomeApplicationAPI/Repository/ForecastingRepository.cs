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
    public async Task<DateTime?> GetLatestActualPriceTimeAsync(string priceArea)
    {
        return await _db.Electricity
            .Where(e => e.PriceArea == priceArea && !e.IsPredicted)
            .MaxAsync(e => (DateTime?)e.Time);
    }

    public async Task<ForecastingResponseDto> PostPrices(ForecastingResponseDto respDto)
    {
        // Tag imod ResponseDto, pak listen ad Predictions ud og tilføj den til database, er jeg ret sikker på
            // Mapping gøres med Select() hvor hver PredictedPricePoint fra respDto pakke ud til 1 Electrity entry
        var existingPredictions = await _db.Electricity
            .Where(e => e.PriceArea == respDto.PriceArea && e.IsPredicted)
            .ToListAsync();

        if (existingPredictions.Count > 0)
        {
            _db.Electricity.RemoveRange(existingPredictions);
        }

        if (respDto.Predictions.Count == 0)
        {
            await _db.SaveChangesAsync();
            return respDto;
        }

        var prices = respDto.Predictions.Select(p => new Electricity
        {
            Time = DateTime.SpecifyKind(p.Time, DateTimeKind.Unspecified),
            PricePerKwh = Math.Round((decimal)p.PricePerKwh, 5),
            PriceArea = p.PriceArea.ToUpperInvariant(),
            IsPredicted = true
        }).ToList();

        await _db.Electricity.AddRangeAsync(prices);
        await _db.SaveChangesAsync();

        return respDto;
    }
}
