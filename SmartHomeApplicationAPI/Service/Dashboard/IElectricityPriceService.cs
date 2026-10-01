using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface IElectricityPriceService
{
    Task<List<HourPriceDto>> GetPricesForDateAsync();
}