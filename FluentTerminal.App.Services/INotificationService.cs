namespace FluentTerminal.App.Services
{
    public interface INotificationService
    {
        void ShowNotification(string title, string content, string url = null);
        void ShowNotificationURL(string title, string content, string url = null);
        void OpenBrowserAsync(string url);
    }
}
