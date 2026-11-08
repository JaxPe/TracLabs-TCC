namespace TracLabs.Frontend.Services.Notification;

public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Error
}

public class NotificationModalModel
{
    public bool IsVisible { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TechnicalDetails { get; set; }
    public string ConfirmButtonText { get; set; } = "Entendi";
}

public interface INotificationModalService
{
    NotificationModalModel CurrentNotification { get; }
    event Action? OnChange;

    void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info, string? technicalDetails = null, string confirmText = "Entendi");
    void ShowError(string title, string message, string? technicalDetails = null);
    void ShowWarning(string title, string message);
    void ShowSuccess(string title, string message);
    void ShowInfo(string title, string message);
    void Close();
}
