using System;
using Windows.ApplicationModel;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Text;
using Windows.Management.Deployment;
using System.Diagnostics;

namespace FluentTerminal.App.Services.Implementation
{
    public class UpdateService : IUpdateService
    {
        private const string apiEndpoint = "https://api.github.com";
        
        private readonly INotificationService _notificationService;
        
        public UpdateService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task CheckForUpdate(bool notifyNoUpdate = false)
        {
            var latest = await GetLatestVersionAsync();
            //_notificationService.ShowNotificationURL("Update available",
            //    "Click to open the update page.", "https://peske.github.io/index.html");
            if (latest > GetCurrentVersion())
            {
                _notificationService.ShowNotificationURL("Update available",
                    "Click to open the update page.", "https://peske.github.io/index.html");
                StartUpdate();
                //_notificationService.ShowNotification("Update available",
                //    "Click to open the release page.", "https://github.com/jumptrading/FluentTerminal/releases");
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

        public async Task<Version> GetLatestVersionAsync_Old()
        {
            var restClient = new RestClient(apiEndpoint);
            var restRequest = new RestRequest("/repos/jumptrading/fluentterminal/releases", Method.GET);
            
            var restResponse = await restClient.ExecuteTaskAsync(restRequest);
            
            if (restResponse.IsSuccessful)
            {
                dynamic restResponseData = JsonConvert.DeserializeObject(restResponse.Content);
                string tag = restResponseData[0].tag_name;
                
                if (tag.Split('-').Length == 3)
                {
                    var latestVersion = new Version(tag.Split('-')[1]);
                    return new Version(latestVersion.Major, latestVersion.Minor, latestVersion.Build, latestVersion.Revision);
                }
            }
            return new Version(0, 0, 0, 0);
        }

        public async Task<Version> GetLatestVersionAsync()
        {
            //  https://peske.github.io/ms-appinstaller:?source=https://peske.github.io/FluentTerminal.App.appinstaller
            var restClient = new RestClient("https://peske.github.io");
            var restRequest = new RestRequest("/FluentTerminal.App.appinstaller", Method.GET);

            var restResponse = await restClient.ExecuteTaskAsync(restRequest);
            string sVersion = "";
            string sXML = "";
            if (restResponse.IsSuccessful)
            {
                sXML = restResponse.Content.Trim();
                string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                if (sXML.StartsWith(_byteOrderMarkUtf8))
                {
                    sXML = sXML.Remove(0, _byteOrderMarkUtf8.Length);
                }
                XmlReader reader = XmlReader.Create(new StringReader(sXML));
                try
                {
                    while (reader.Read())
                    {
                        // Only detect start elements.
                        if (reader.IsStartElement())
                        {
                            if (reader.Name == "AppInstaller")
                            {
                                string attribute = reader["Version"];
                                if (attribute != null)
                                {
                                    sVersion = attribute;
                                    break;
                                }
                            }
                        }
                    }

                    reader.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    reader.Dispose();
                }

                if (sVersion != "")
                    return new Version(sVersion);
            }
            return new Version(0, 0, 0, 0);
        }
        public async Task<string> GetBundleLocationAsync()
        {
            //  https://peske.github.io/ms-appinstaller:?source=https://peske.github.io/FluentTerminal.App.appinstaller
            var restClient = new RestClient("https://peske.github.io");
            var restRequest = new RestRequest("/FluentTerminal.App.appinstaller", Method.GET);

            var restResponse = await restClient.ExecuteTaskAsync(restRequest);
            string sBundleLocation = "";
            string sXML = "";
            if (restResponse.IsSuccessful)
            {
                sXML = restResponse.Content.Trim();
                string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                if (sXML.StartsWith(_byteOrderMarkUtf8))
                {
                    sXML = sXML.Remove(0, _byteOrderMarkUtf8.Length);
                }
                XmlReader reader = XmlReader.Create(new StringReader(sXML));
                try
                {
                    while (reader.Read())
                    {
                        // Only detect start elements.
                        if (reader.IsStartElement())
                        {
                            if (reader.Name == "MainBundle")
                            {
                                string attribute = reader["Uri"];
                                if (attribute != null)
                                {
                                    sBundleLocation = attribute;
                                    break;
                                }
                            }
                        }
                    }

                    reader.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    reader.Dispose();
                }
            }
            return sBundleLocation;
        }

        public async void StartUpdate()
        {
            try
            {
                _notificationService.OpenBrowserAsync("ms-appinstaller:?source=https://peske.github.io/FluentTerminal.App.appinstaller");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
