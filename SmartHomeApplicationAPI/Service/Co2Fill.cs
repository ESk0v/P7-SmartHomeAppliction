namespace SmartHomeApplicationAPI.Service;

public static class Co2Fill
{
    // Fills each missing CO2 value with the value at the same time 24 hours earlier, found by timestamp
    // (so missing rows do not shift the lookup). times must be ascending; returns how many were filled.
    public static int SeasonalNaive(IReadOnlyList<DateTime> times, List<double?> co2)
    {
        if (times.Count != co2.Count)
            throw new ArgumentException("times and co2 must have the same length.");

        var indexByTime = new Dictionary<DateTime, int>(times.Count);
        for (var i = 0; i < times.Count; i++)
            indexByTime[times[i]] = i;

        var filled = 0;
        for (var i = 0; i < co2.Count; i++)
        {
            if (co2[i] is not null)
                continue;

            // Ascending order means the day-before value is already filled when it was itself missing
            if (indexByTime.TryGetValue(times[i].AddDays(-1), out var j) && co2[j] is { } earlier)
            {
                co2[i] = earlier;
                filled++;
            }
        }
        return filled;
    }
}
