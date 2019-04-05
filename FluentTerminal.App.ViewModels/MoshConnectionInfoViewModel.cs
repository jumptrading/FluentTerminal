using FluentTerminal.App.Services.Dialogs;

namespace FluentTerminal.App.ViewModels
{
    public class MoshConnectionInfoViewModel : SshConnectionInfoViewModel, IMoshConnectionInfo
    {
        private string _moshPorts = "60000:60050";

        public string MoshPorts
        {
            get => _moshPorts;
            set => Set(ref _moshPorts, value);
        }

        private string _password;

        public string Password
        {
            get => _password;
            set => Set(ref _password, value);
        }
    }
}