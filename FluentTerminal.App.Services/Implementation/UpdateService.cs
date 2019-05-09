using System;
using Windows.ApplicationModel;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;

namespace FluentTerminal.App.Services.Implementation
{
    public class UpdateService : IUpdateService
    {
        private const string apiEndpoint = "https://api.github.com";

        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly ITrayProcessCommunicationService _trayProcessCommunicationService;

        private async Task<IRestResponse> SendGitHubAPIRequest(string request)
        {
            var restClient = new RestClient(apiEndpoint);
            var restRequest = new RestRequest(request, Method.GET);
            return await restClient.ExecuteTaskAsync(restRequest);
        }

        private async Task<string> GetInstallerURL(string releaseTag)
        {
            var restResponse = await SendGitHubAPIRequest($"/repos/jumptrading/fluentterminal/releases/tags/{releaseTag}");
            if (restResponse.IsSuccessful)
            {
                try
                {
                    dynamic restResponseData = JsonConvert.DeserializeObject(restResponse.Content);
                    var url = ((IEnumerable)restResponseData["assets"]).Cast<dynamic>().FirstOrDefault(asset => ((string)asset["browser_download_url"]).EndsWith(".msi"))["browser_download_url"];
                    return url;
                }
                catch (Exception) { }
            }
            return "";
        }

        public UpdateService(INotificationService notificationService, IDialogService dialogService, ITrayProcessCommunicationService trayProcessCommunicationService)
        {
            _notificationService = notificationService;
            _dialogService = dialogService;
            _trayProcessCommunicationService = trayProcessCommunicationService;
        }

        public async Task CheckForUpdate(bool notifyNoUpdate = false)
        {
            var latest = await GetLatestVersionAsync();
            if (latest > GetCurrentVersion())
            {
                _notificationService.ShowNotification("Update available",
                    "Click to open the releases page.", "https://github.com/jumptrading/FluentTerminal/releases");

                var installerFileUrl = await GetInstallerURL(latest.ToString(4));
                if (!string.IsNullOrEmpty(installerFileUrl))
                {
                    DialogButton result = await _dialogService.ShowMessageDialogAsnyc("Update is available.", "Application will be closed during update. Press OK to start the update.", new DialogButton[] { DialogButton.OK, DialogButton.Cancel });
                    if (result == DialogButton.OK)
                    {
                        _trayProcessCommunicationService.StartApplicationUpdate(installerFileUrl);
                    }
                }
            }
            else if (notifyNoUpdate)
            {
                _notificationService.ShowNotification("No update available", "You're up to date!");
            }
        }

        public Version GetCurrentVersion()
        {
            var currentVersion = Package.Current.Id.Version;
            return new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build, currentVersion.Revision);
        }

        public async Task<Version> GetLatestVersionAsync()
        {
            var restResponse = await SendGitHubAPIRequest("/repos/jumptrading/fluentterminal/releases");
            if (restResponse.IsSuccessful)
            {
                dynamic restResponseData = JsonConvert.DeserializeObject(restResponse.Content);
                string tag = restResponseData[0].tag_name;
                var latestVersion = new Version(tag);
                return new Version(latestVersion.Major, latestVersion.Minor, latestVersion.Build, latestVersion.Revision);
            }
            return new Version(0, 0, 0, 0);
        }
    }
}
