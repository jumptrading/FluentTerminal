using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FluentTerminal.App.Services.Dialogs;
using FluentTerminal.App.Utilities;
using FluentTerminal.App.Services;
using FluentTerminal.App.ViewModels;

namespace FluentTerminal.App.Dialogs
{
    // ReSharper disable once RedundantExtendsListEntry
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

        private async void BrowseButtonOnClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker {SuggestedStartLocation = PickerLocationId.DocumentsLibrary};

            openPicker.FileTypeFilter.Add("*");

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file != null)
                ((SshConnectionInfoViewModel) DataContext).IdentityFile = file.Path;
        }
    }
}
