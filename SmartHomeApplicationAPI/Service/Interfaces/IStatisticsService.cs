using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;
public interface IStatisticsService
{
    // Dashboard Statistics
    Task<StatisticsDto> GetPlannedCostAsync(string userID, DateTime from, DateTime to);
    Task<StatisticsDto> GetPlannedCo2Async(string userID, DateTime from, DateTime to);
    Task<StatisticsDto> GetScheduleSavingCostAsync(string userID, DateTime from, DateTime to);
    Task<StatisticsDto> GetScheduleSavingCo2Async(string userID, DateTime from, DateTime to);

    // Optimization Plan Statistics
    Task<StatisticsDto> GetEstimatedCostAsync(string userID, int planId);
    Task<StatisticsDto> GetMoneySavedAsync(string userID, int planId);
    Task<StatisticsDto> GetLoadDurationAsync(string userID, int planId);
    Task<StatisticsDto> GetLoadFinishAsync(string userID, int planId);
}