using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface IStatisticsService
{
    Task<StatisticsDto> GetPlannedCostAsync(string userID);
}