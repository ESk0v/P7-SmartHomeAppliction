using Utilities.Models;
using SmartHomeApplicationAPI.Repository;

namespace SmartHomeApplicationAPI.Service;

public class ElectricityPriceService : IElectricityPriceService
{
    private readonly IElectricityPriceRepository _repository;

    public ElectricityPriceService(IElectricityPriceRepository repository)
    {
        _repository = repository;
    }
    public async Task<List<HourPriceDto>> GetPricesForDateAsync(string areaCode)
    {
        DateTime date = DateTime.Now;

        var Result = await _repository.GetPricesForDateAsync(areaCode, date);

        return Result
            .GroupBy(r => new DateTime(r.Hour.Year, r.Hour.Month, r.Hour.Day, r.Hour.Hour, 0, 0))
            .Select(g => new HourPriceDto(g.Key, g.Sum(r => r.Price) / g.Count()))
            .OrderBy(r => r.Hour)
            .ToList();
    }
}