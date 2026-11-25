using Microsoft.JSInterop;

namespace SmartHomeApplication.State;

public sealed class ChartMetricPreference(IJSRuntime js)
{
    private readonly Dictionary<string, bool> preferences = new();

    public async Task<bool> GetAsync(string userId)
    {
        if (preferences.TryGetValue(userId, out bool cached)) return cached;

        bool showCo2 = false;
        try
        {
            string? stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey(userId));
            bool.TryParse(stored, out showCo2);
        }
        catch (JSException) { /* Keep navigation working when browser storage is unavailable. */ }

        // A manual choice made while storage was loading takes precedence.
        preferences.TryAdd(userId, showCo2);
        return preferences[userId];
    }

    public async Task SetAsync(string userId, bool showCo2)
    {
        preferences[userId] = showCo2;
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey(userId), showCo2.ToString());
        }
        catch (JSException) { /* The scoped preference still survives page navigation. */ }
    }

    private static string StorageKey(string userId) => $"smartHome.chart.showCo2:{userId}";
}
