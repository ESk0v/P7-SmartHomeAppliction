using Utilities.Models;
using SmartHomeApplicationAPI.Repository;

namespace SmartHomeApplicationAPI.Service;

public class StatisticsService : IStatisticsService
{
    private readonly IStatisticsRepository _repository;

    public StatisticsService(IStatisticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<StatisticsDto> GetPlannedCostAsync(string userID)
    {
        var start = DateTime.Today;
        var end = start.AddDays(1);

        #return await _repository.GetPlannedCostAsync(userID, start, end);
    }
}