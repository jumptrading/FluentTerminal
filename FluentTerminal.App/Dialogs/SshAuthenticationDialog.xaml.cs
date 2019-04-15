using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FluentTerminal.App.Services;
using FluentTerminal.App.Services.Dialogs;
using FluentTerminal.App.Utilities;
using FluentTerminal.App.ViewModels;
using FluentTerminal.Models;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FluentTerminal.App.Dialogs
{
    // ReSharper disable once RedundantExtendsListEntry
    public sealed partial class SshAuthenticationDialog : ContentDialog, ISshAuthenticationDialog
    {
        public SshAuthenticationDialog(ISettingsService settingsService)
        {
            InitializeComponent();
            var currentTheme = settingsService.GetCurrentTheme();
            RequestedTheme = ContrastHelper.GetIdealThemeForBackgroundColor(currentTheme.Colors.Background);
        }

        private async void BrowseButtonOnClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };

            openPicker.FileTypeFilter.Add("*");

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file != null)
                ((SshConnectionInfoViewModel)DataContext).IdentityFile = file.Path;
        }

        public async Task<SshConnectionInfo> GetSshAuthenticationAsync(ISshConnectionInfo sshConnectionInfo)
        {
            ((SshConnectionInfoViewModel) DataContext).CopyFrom(sshConnectionInfo);

            ContentDialogResult result = await ShowAsync();

            if (result != ContentDialogResult.Primary)
                return null;

            SshConnectionInfo sshInfo = new SshConnectionInfo();

            ((SshConnectionInfoViewModel) DataContext).CopyTo(sshInfo);

            return sshInfo;
        }
    }
}
