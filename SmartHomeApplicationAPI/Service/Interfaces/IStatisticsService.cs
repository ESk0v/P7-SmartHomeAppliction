using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface IStatisticsService
{
    Task<StatisticsDto> GetPlannedCostAsync(string userID, DateTime from, DateTime to);
    Task<StatisticsDto> GetPlannedCo2Async(string userID, DateTime from, DateTime to);
    Task<StatisticsDto> GetScheduleSavingCostAsync(string userID, DateTime from, DateTime to);
    Task<StatisticsDto> GetScheduleSavingCo2Async(string userID, DateTime from, DateTime to);
}