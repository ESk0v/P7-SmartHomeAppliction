public interface INotificationService
{
    event Action<string, ToastLevel>? OnShow;
    event Action? OnHide;

    const string SuccessMsg = "Success";
    const string ConnectionErrorMsg = "Connection error occurred";
    const string ServerErrorMsg = "Server error occurred";
    const string WarningMsg = "Warning";

    void ShowNotification(string message, ToastLevel level, int CloseAfter = 8000);
    void SuccessNotification(string? message = SuccessMsg);
    void ErrorNotification(string? message = ServerErrorMsg);
    void InfoNotification(string? message = ConnectionErrorMsg);
    void WarningNotification(string? message = null);
}