using Utilities.Models;
using SmartHomeApplicationAPI.Repository;

namespace SmartHomeApplicationAPI.Service;

public class ElectricityPriceService : IElectricityPriceService
{
    private readonly IElectricityPriceRepository _repository;

    public ElectricityPriceService(IElectricityPriceRepository repository)
    {
        _repository = repository;
    }
    public async Task<List<HourPriceDto>> GetPricesForDateAsync(string areaCode)
    {
        DateTime date = DateTime.Now;
        DateTime updatedDate = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerHour));

        var Result = await _repository.GetPricesForDateAsync(areaCode, updatedDate);

        return Result
            .GroupBy(r => new DateTime(r.Hour.Year, r.Hour.Month, r.Hour.Day, r.Hour.Hour, 0, 0))
            .Select(g => new HourPriceDto(
                g.Key,
                g.Average(r => r.Price)))
            .OrderBy(r => r.Hour)
            .ToList();
    }

    public async Task<UnscheduledEstimateResultDto> GetUnscheduledEstimateAsync(
        UnscheduledEstimateRequestDto request)
    {
        DateTime localStart = ToCopenhagenTime(request.StartTime);
        DateTime localEnd = ToCopenhagenTime(request.EndTime);

        List<ElectricityReadingDto> readings = await _repository.GetReadingsAsync(
            request.AreaCode,
            localStart,
            localEnd);

        if (readings.Count == 0)
        {
            return new UnscheduledEstimateResultDto(
                null,
                "No electricity data is available for the unscheduled baseline period.");
        }

        decimal averagePrice = readings.Average(reading => reading.PricePerKwh);
        decimal? averageCo2 = readings
            .Where(reading => reading.Co2EmissionGramsPerKwh.HasValue)
            .Average(reading => reading.Co2EmissionGramsPerKwh);

        return new UnscheduledEstimateResultDto(
            new UnscheduledEstimateDto(
                request.EnergyInKwh * averagePrice,
                // Temporary sentinel until CO2 forecast data is available for the full horizon.
                request.EnergyInKwh * (averageCo2 ?? -1m)),
            null);
    }

    private static DateTime ToCopenhagenTime(DateTime utcTime)
    {
        TimeZoneInfo copenhagenTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        // Copenhagen is UTC+1 normally and UTC+2 during daylight saving time.
        int daylightSavingOffset = copenhagenTimeZone.IsDaylightSavingTime(
            utcTime)
            ? 2
            : 1;

        return DateTime.SpecifyKind(utcTime.AddHours(daylightSavingOffset), DateTimeKind.Unspecified);
    }
}