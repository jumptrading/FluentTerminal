using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using FluentTerminal.App.Services;
using FluentTerminal.App.Services.Dialogs;
using FluentTerminal.App.Utilities;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FluentTerminal.App.Dialogs
{
    // ReSharper disable once RedundantExtendsListEntry
    public sealed partial class MoshInfoDialog : ContentDialog, IMoshConnectionInfoDialog
    {
        public MoshInfoDialog(ISettingsService settingsService)
        {
            InitializeComponent();
            var currentTheme = settingsService.GetCurrentTheme();
            RequestedTheme = ContrastHelper.GetIdealThemeForBackgroundColor(currentTheme.Colors.Background);
        }

        public async Task<IMoshConnectionInfo> GetMoshConnectionInfoAsync()
        {
            ContentDialogResult result = await ShowAsync();

            return result == ContentDialogResult.Primary ? (IMoshConnectionInfo)DataContext : null;
        }
    }
}
