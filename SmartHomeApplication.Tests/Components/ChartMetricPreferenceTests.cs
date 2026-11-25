using Microsoft.JSInterop;
using SmartHomeApplication.State;

namespace SmartHomeApplication.Tests.Components;

public class ChartMetricPreferenceTests
{
    [Fact]
    public async Task ManualChoice_IsRestoredInANewScope_AndBelongsToItsAccount()
    {
        var js = new StorageJs();

        await new ChartMetricPreference(js).SetAsync("alice", true);
        var nextScope = new ChartMetricPreference(js);

        Assert.True(await nextScope.GetAsync("alice"));
        Assert.False(await nextScope.GetAsync("bob"));
    }

    [Fact]
    public async Task UnavailableBrowserStorage_StillRemembersChoicesDuringNavigation()
    {
        var preference = new ChartMetricPreference(new StorageJs { Unavailable = true });

        Assert.False(await preference.GetAsync("alice"));
        await preference.SetAsync("alice", true);
        Assert.True(await preference.GetAsync("alice"));
    }

    private sealed class StorageJs : IJSRuntime
    {
        private readonly Dictionary<string, string> storage = new();
        public bool Unavailable { get; init; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (Unavailable) throw new JSException("Storage disabled");
            string key = (string)args![0]!;
            if (identifier == "localStorage.getItem")
                return ValueTask.FromResult((TValue)(object?)storage.GetValueOrDefault(key)!);

            Assert.Equal("localStorage.setItem", identifier);
            storage[key] = (string)args[1]!;
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
