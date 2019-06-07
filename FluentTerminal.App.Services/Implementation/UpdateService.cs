using System;
using Windows.ApplicationModel;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using System.Net;
using System.IO;
using FluentTerminal.Models;

namespace FluentTerminal.App.Services.Implementation
{
    public class UpdateService : IUpdateService
    {
        private const string apiEndpoint = "https://api.github.com";

        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly ITrayProcessCommunicationService _trayProcessCommunicationService;
        private readonly ISettingsService _settingsService;

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

        public UpdateService(INotificationService notificationService, IDialogService dialogService, ITrayProcessCommunicationService trayProcessCommunicationService, ISettingsService settingsService)
        {
            _notificationService = notificationService;
            _dialogService = dialogService;
            _trayProcessCommunicationService = trayProcessCommunicationService;
            _settingsService = settingsService;
        }

        private async Task<string> DownloadInstaller(string version, string url)
        {
            try
            {
                string tempFilePath = Path.GetTempFileName();
                WebClient webClient = new WebClient();

                _notificationService.ShowNotification("New version download is started", $"Installer for version {version} will be downloaded.");

                await webClient.DownloadFileTaskAsync(url, tempFilePath);

                _settingsService.SaveAutoUpdateData(version, tempFilePath);

                _notificationService.ShowNotification("Download successful", $"Installer for version {version} was downloaded.");

                return tempFilePath;
            }
            catch (Exception)
            {
                _notificationService.ShowNotification("Download failed", $"Download failed for installer of version {version}.");
                Logger.Instance.Error($"Download error for version {version} {url}");
                return string.Empty;
            }
        }

        private void runInstaller(string version, string msiPath)
        {
            _notificationService.ShowNotification("Update is started", $"Version {version} will be installed.");

            _trayProcessCommunicationService.RunMSI(msiPath);
        }

        private async Task<DialogButton> PromptStartUpdate(Version newVersion)
        {
            return await _dialogService.ShowMessageDialogAsnyc($"New application build {newVersion} is available.",
                            "Update will be downloaded and installed immediately. All terminal sessions will be closed with potential risk to lose important sessions' data. Press OK to start the update.",
                            new DialogButton[] { DialogButton.OK, DialogButton.Cancel }).ConfigureAwait(true);
        }

        public async Task CheckForUpdate(bool runUpdate = false)
        {
            try
            {
                bool autoUpdatesAllowed = _settingsService.GetApplicationSettings().AutoInstallUpdates;
                bool canRunInstaller = runUpdate || !autoUpdatesAllowed;
                ApplicationVersionUpgradeData updateData = _settingsService.GetAutoUpdateData();
                Version downloaded = updateData.Version;
                Version current = GetCurrentVersion();
                Version latest = await GetLatestVersionAsync();

                if (downloaded > current && downloaded >= latest)
                {
                    DialogButton result = autoUpdatesAllowed ? DialogButton.OK : await PromptStartUpdate(latest);
                    if (result == DialogButton.OK && canRunInstaller)
                    {
                        runInstaller(downloaded.ToString(4), updateData.Path);
                    }
                }
                else if (latest > current)
                {
                    var installerFileUrl = await GetInstallerURL(latest.ToString(4));
                    if (!string.IsNullOrEmpty(installerFileUrl))
                    {
                        DialogButton result = autoUpdatesAllowed ? DialogButton.OK : await PromptStartUpdate(latest);
                        if (result == DialogButton.OK)
                        {
                            string installerFilePath = await DownloadInstaller(latest.ToString(4), installerFileUrl);
                            if (canRunInstaller && !string.IsNullOrEmpty(installerFilePath))
                            {
                                runInstaller(latest.ToString(4), installerFilePath);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
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
