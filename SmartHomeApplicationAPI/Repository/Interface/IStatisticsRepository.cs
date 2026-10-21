using Utilities.Models;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Repository;

public interface IStatisticsRepository
{
    Task<List<Plan>> RetrievePlansAsync(string UserID, DateTime start, DateTime end);
}