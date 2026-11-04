using System.Timers;

public class NotificationService : INotificationService, IDisposable
{
    public event Action<string, ToastLevel>? OnShow;
    public event Action? OnHide;

    private System.Timers.Timer? _countdowntimer;
    private const int DefaultCloseTimer = 8000;

    public void SuccessNotification(string? message = INotificationService.SuccessMsg)
    => ShowNotification(message ?? INotificationService.SuccessMsg, ToastLevel.Success);

    public void ErrorNotification(string? message = INotificationService.ServerErrorMsg)
        => ShowNotification(message ?? INotificationService.ServerErrorMsg, ToastLevel.Error);

    public void WarningNotification(string? message = null)
        => ShowNotification(message ?? INotificationService.WarningMsg, ToastLevel.Warning);

    public void InfoNotification(string? message = null)
        => ShowNotification(message ?? INotificationService.ConnectionErrorMsg, ToastLevel.Info);

    public void ShowNotification(string message, ToastLevel Level, int closeTimer = DefaultCloseTimer)
    {
        OnShow?.Invoke(message, Level);
        StartCountdown(closeTimer);
    }

    private void StartCountdown(int timer)
    {
        if (_countdowntimer == null)
        { 
        _countdowntimer = new System.Timers.Timer(timer);
        _countdowntimer.Elapsed += HideNotification;
        _countdowntimer.AutoReset = false;
        }

        if (_countdowntimer.Enabled)
        {
            _countdowntimer.Stop();
        }
        
        _countdowntimer.Interval = timer;
        _countdowntimer.Start();
    }

    private void HideNotification(object? sender, ElapsedEventArgs e)
    {
        OnHide?.Invoke();
    }

    public void Dispose()
    {
        _countdowntimer?.Dispose();
    }
}