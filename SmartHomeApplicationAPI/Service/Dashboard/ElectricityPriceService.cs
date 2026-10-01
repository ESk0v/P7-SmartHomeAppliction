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
        return await _repository.GetPricesForDateAsync(areaCode, date);
        
    }
}