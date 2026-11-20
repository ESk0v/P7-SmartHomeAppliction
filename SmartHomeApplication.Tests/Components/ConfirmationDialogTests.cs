using System.Reflection;
using Microsoft.JSInterop;
using SmartHomeApplication.Components.Shared;

namespace SmartHomeApplication.Tests.Components;

public class ConfirmationDialogTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Confirm_PreservesTheChoice_WhenTheBrowserCloseEventFiresFirst(bool confirmed)
    {
        var dialog = new ConfirmationDialog();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var completion = new TaskCompletionSource<bool>();
        typeof(ConfirmationDialog).GetField("pending", flags)!.SetValue(dialog, completion);
        typeof(ConfirmationDialog).GetProperty("JS", flags)!.SetValue(dialog,
            new ClosingJs(() => typeof(ConfirmationDialog).GetMethod("Closed", flags)!.Invoke(dialog, null)));

        await (Task)typeof(ConfirmationDialog).GetMethod("Complete", flags)!.Invoke(dialog, new object[] { confirmed })!;

        Assert.Equal(confirmed, await completion.Task.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    private sealed class ClosingJs(Action close) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Assert.Equal("smartHomeAccessibility.closeDialog", identifier);
            close();
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
