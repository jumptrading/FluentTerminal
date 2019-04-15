using FluentTerminal.App.Services.Dialogs;
using GalaSoft.MvvmLight;

namespace FluentTerminal.App.ViewModels
{
    public class SshConnectionInfoViewModel : ViewModelBase, ISshConnectionInfo
    {
        private string _host = string.Empty;

        public string Host
        {
            get => _host;
            set => Set(ref _host, value);
        }

        private ushort _sshPort = 22;

        public ushort SshPort
        {
            get => _sshPort;
            set => Set(ref _sshPort, value);
        }

        private string _username = string.Empty;

        public string Username
        {
            get => _username;
            set => Set(ref _username, value);
        }

        private string _identityFile = string.Empty;

        public string IdentityFile
        {
            get => _identityFile;
            set => Set(ref _identityFile, value);
        }

        private bool _useMosh;

        public bool UseMosh
        {
            get => _useMosh;
            set => Set(ref _useMosh, value);
        }

        private string _password = string.Empty;

        public string Password
        {
            get => _password;
            set => Set(ref _password, value);
        }

        private string _passphrase = string.Empty;

        public string Passphrase
        {
            get => _passphrase;
            set => Set(ref _passphrase, value);
        }

        private string _moshPorts = "60000:60050";

        public string MoshPorts
        {
            get => _moshPorts;
            set => Set(ref _moshPorts, value);
        }
    }
}