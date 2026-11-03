using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface IStatisticsService
{
    Task<StatisticsDto> GetPlannedCostAsync(string userID);

    Task<StatisticsDto> GetScheduleSavingCostAsync(string userID);

    Task<StatisticsDto> GetPlannedCo2Async(string userID);

    Task<StatisticsDto> GetScheduleSavingCo2Async(string userID);
}