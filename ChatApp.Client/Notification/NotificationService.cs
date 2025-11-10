using MudBlazor;

namespace ChatApp.Client.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly ISnackbar _snackbar;

        public NotificationService(ISnackbar snackbar)
        {
            _snackbar = snackbar;
        }

        public void ShowSuccess(string message)
        {
            _snackbar.Add(message, Severity.Success);
        }

        public void ShowError(string message)
        {
            _snackbar.Add(message, Severity.Error);
        }

        public void ShowWarning(string message)
        {
            _snackbar.Add(message, Severity.Warning);
        }

        public void ShowInfo(string message)
        {
            _snackbar.Add(message, Severity.Info);
        }
    }
}