using System;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls;
using FluentTerminal.App.Services.Dialogs;
using FluentTerminal.App.Utilities;
using FluentTerminal.App.Services;

namespace FluentTerminal.App.Dialogs
{
    public sealed partial class SshInfoDialog : ContentDialog, ISshConnectionInfoDialog
    {
        public SshInfoDialog(ISettingsService settingsService)
        {
            InitializeComponent();
            var currentTheme = settingsService.GetCurrentTheme();
            RequestedTheme = ContrastHelper.GetIdealThemeForBackgroundColor(currentTheme.Colors.Background);
        }

        public async Task<ISshConnectionInfo> GetSshConnectionInfoAsync()
        {
            ContentDialogResult result = await ShowAsync();

            return result == ContentDialogResult.Primary ? (ISshConnectionInfo)DataContext : null;
        }

        private void SshInfoDialog_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.ContentDialog", "DefaultButton"))
            {
                this.DefaultButton = ContentDialogButton.Primary;
            }
        }
    }
}
