using System;
using Windows.ApplicationModel;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
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

        private async void StartDownload(string version, string url)
        {
            try
            {
                string tempFilePath = Path.GetTempFileName();
                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(url, tempFilePath);
                _settingsService.SaveAutoUpdateData(version, tempFilePath);
            }
            catch (Exception ex)
            {
                //LogException("Download Error", ex);
            }
        }

        public async Task CheckForUpdate(bool notifyNoUpdate = false)
        {
            try
            {
                ApplicationVersionUpgradeData updateData = _settingsService.GetAutoUpdateData();
                Version downloaded = updateData.Version;
                Version current = new Version("0.0.0.0"); // GetCurrentVersion();
                Version latest = await GetLatestVersionAsync();

                if (downloaded > current && downloaded >= latest)
                {
                    _notificationService.ShowNotification("Update will be installed", "Installation of new application version is started.");

                    _trayProcessCommunicationService.RunMSI(updateData.Path);
                }
                else if (latest > current)
                {
                    _notificationService.ShowNotification("Update available",
                        "Click to open the releases page.", "https://github.com/jumptrading/FluentTerminal/releases");

                    var installerFileUrl = await GetInstallerURL(latest.ToString(4));
                    if (!string.IsNullOrEmpty(installerFileUrl))
                    {
                        DialogButton result = await _dialogService.ShowMessageDialogAsnyc("Update is available.", "Application update will be downloaded and installed on next application launch. Press OK to start the download.", new DialogButton[] { DialogButton.OK, DialogButton.Cancel }).ConfigureAwait(true);
                        if (result == DialogButton.OK)
                        {
                            StartDownload(latest.ToString(4), installerFileUrl);
                        }
                    }
                }
                else if (notifyNoUpdate)
                {
                    _notificationService.ShowNotification("No update available", "You're up to date!");
                }
            }
            catch(Exception ex)
            {

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
