using Utilities.Models;
using SmartHomeApplicationAPI.Repository;

namespace SmartHomeApplicationAPI.Service;

public class HistoryService : IHistoryService
{
    private static readonly TimeZoneInfo CopenhagenTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private readonly IHistoryRepository _repository;

    public HistoryService(IHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<HistoryPointDto>> GetHistoryAsync(string userID, DateTime from, DateTime to)
    {
        var plans = await _repository.RetrievePlanSummariesAsync(userID, ToUtc(from), ToUtc(to));

        var totals = plans
            .GroupBy(p => TimeZoneInfo.ConvertTimeFromUtc(p.StartTime, CopenhagenTz).Date)
            .ToDictionary(
                g => g.Key,
                g => (Cost: g.Sum(p => p.Price), Co2: g.Sum(p => p.Co2)));

        var result = new List<HistoryPointDto>();
        for (var day = from.Date; day < to.Date; day = day.AddDays(1))
        {
            totals.TryGetValue(day, out var t);
            result.Add(new HistoryPointDto(day, t.Cost, t.Co2));
        }

        return result;
    }

    private static DateTime ToUtc(DateTime localDate) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified), CopenhagenTz);
}