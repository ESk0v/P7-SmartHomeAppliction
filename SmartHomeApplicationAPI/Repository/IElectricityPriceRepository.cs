using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IElectricityPriceRepository
{
    Task<List<HourPriceDto>> GetPricesForDateAsync(string areaCode, DateTime date);
}