using SmartHomeApplicationAPI.Service;
using Xunit;

namespace SmartHomeApplicationAPI.Tests.Service;

public class Co2FillTests
{
    private const int Day = 96;
    private static readonly DateTime Origin = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    private static List<DateTime> Times(int count) =>
        Enumerable.Range(0, count).Select(i => Origin.AddMinutes(15 * i)).ToList();

    [Fact]
    public void SeasonalNaive_GapShorterThanOneDay_FillsFromSameQuarterHourTheDayBefore()
    {
        // One full day of values, then a day where the last 10 quarter-hours are missing
        var co2 = Enumerable.Range(0, Day).Select(i => (double?)i).ToList();
        co2.AddRange(Enumerable.Range(0, Day).Select(i => i < Day - 10 ? (double?)1000 + i : null));

        var filled = Co2Fill.SeasonalNaive(Times(co2.Count), co2);

        Assert.Equal(10, filled);
        for (var i = Day - 10; i < Day; i++)
            Assert.Equal(i, co2[Day + i]);
        Assert.Equal(1000, co2[Day]);
    }

    [Fact]
    public void SeasonalNaive_GapLongerThanOneDay_FillsFromFilledValues()
    {
        // One day of values, then two and a half days without any
        var co2 = Enumerable.Range(0, Day).Select(i => (double?)i).ToList();
        co2.AddRange(Enumerable.Repeat<double?>(null, Day * 2 + Day / 2));

        var filled = Co2Fill.SeasonalNaive(Times(co2.Count), co2);

        Assert.Equal(Day * 2 + Day / 2, filled);
        for (var i = Day; i < co2.Count; i++)
            Assert.Equal(i % Day, co2[i]);
    }

    [Fact]
    public void SeasonalNaive_NoValues_FillsNothing()
    {
        var co2 = Enumerable.Repeat<double?>(null, Day * 2).ToList();

        var filled = Co2Fill.SeasonalNaive(Times(co2.Count), co2);

        Assert.Equal(0, filled);
        Assert.All(co2, g => Assert.Null(g));
    }

    [Fact]
    public void SeasonalNaive_MissingRowsInHistory_FillsFromMatchingTimestamp()
    {
        // History day with 6 rows removed, then a full day with all CO2 missing
        var times = Times(Day * 2);
        var co2 = Enumerable.Range(0, Day * 2).Select(i => i < Day ? (double?)i : null).ToList();
        var removed = new HashSet<int> { 10, 11, 12, 13, 14, 15 };
        times = times.Where((_, i) => !removed.Contains(i)).ToList();
        co2 = co2.Where((_, i) => !removed.Contains(i)).ToList();

        var filled = Co2Fill.SeasonalNaive(times, co2);

        Assert.Equal(Day - removed.Count, filled);
        for (var i = 0; i < times.Count; i++)
        {
            if (times[i] < Origin.AddDays(1))
                continue;
            var quarter = (int)((times[i] - Origin.AddDays(1)).TotalMinutes / 15);
            if (removed.Contains(quarter))
                Assert.Null(co2[i]);                // no value yesterday at that time
            else
                Assert.Equal(quarter, co2[i]);      // same quarter-hour, not shifted by the gap
        }
    }
}
