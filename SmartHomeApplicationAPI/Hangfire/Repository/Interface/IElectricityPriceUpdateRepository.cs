using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public interface IElectricityPriceUpdateRepository
{
    Task<List<Electricity>> GetPricesAsync(
        DateTime from,
        DateTime to);

    Task SavePricesAsync(
        IReadOnlyCollection<Electricity> newPrices);
}