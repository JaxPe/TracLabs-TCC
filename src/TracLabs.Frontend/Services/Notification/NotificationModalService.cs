namespace TracLabs.Frontend.Services.Notification;

public class NotificationModalService : INotificationModalService
{
    public NotificationModalModel CurrentNotification { get; } = new();

    public event Action? OnChange;

    public void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info, string? technicalDetails = null, string confirmText = "Entendi")
    {
        CurrentNotification.Title = title;
        CurrentNotification.Message = message;
        CurrentNotification.Severity = severity;
        CurrentNotification.TechnicalDetails = technicalDetails;
        CurrentNotification.ConfirmButtonText = confirmText;
        CurrentNotification.IsVisible = true;

        NotifyStateChanged();
    }

    public void ShowError(string title, string message, string? technicalDetails = null)
    {
        Show(title, message, NotificationSeverity.Error, technicalDetails, "Fechar");
    }

    public void ShowWarning(string title, string message)
    {
        Show(title, message, NotificationSeverity.Warning, null, "Entendi");
    }

    public void ShowSuccess(string title, string message)
    {
        Show(title, message, NotificationSeverity.Success, null, "Ótimo");
    }

    public void ShowInfo(string title, string message)
    {
        Show(title, message, NotificationSeverity.Info, null, "OK");
    }

    public void Close()
    {
        CurrentNotification.IsVisible = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
