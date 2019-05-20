using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using FluentTerminal.App.Services;
using FluentTerminal.Models;
using FluentTerminal.Models.Enums;
using GalaSoft.MvvmLight;

namespace FluentTerminal.App.ViewModels
{
    public class SshConnectionInfoViewModel : ViewModelBase, ISshConnectionInfo
    {
        #region Constants

        public const ushort DefaultSshPort = 22;
        public const ushort DefaultMoshPortsFrom = 60001;
        public const ushort DefaultMoshPortsTo = 60999;

        private const string MoshExe = "mosh.exe";

        #endregion Constants

        #region Static

        private static readonly Lazy<string> SshExeLocationLazy = new Lazy<string>(() =>
        {
            //
            // See https://stackoverflow.com/a/25919981
            //

            string system32Folder;

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                system32Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Sysnative");
            }
            else
            {
                system32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            }

            return Path.Combine(system32Folder, @"OpenSSH\ssh.exe");
        });

        #endregion Static

        #region Properties

        private string _host = string.Empty;

        public string Host
        {
            get => _host;
            set => Set(ref _host, value);
        }

        private ushort _sshPort = DefaultSshPort;

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

        private ushort _moshPortFrom = DefaultMoshPortsFrom;

        public ushort MoshPortFrom
        {
            get => _moshPortFrom;
            set => Set(ref _moshPortFrom, value);
        }

        private ushort _moshPortTo = DefaultMoshPortsTo;

        public ushort MoshPortTo
        {
            get => _moshPortTo;
            set => Set(ref _moshPortTo, value);
        }

        private LineEndingStyle _lineEndingStyle;

        public LineEndingStyle LineEndingStyle
        {
            get => _lineEndingStyle;
            set => Set(ref _lineEndingStyle, value);
        }

        public ObservableCollection<SshOptionViewModel> SshOptions { get; }

        #endregion Properties

        #region Constructors

        public SshConnectionInfoViewModel(LineEndingStyle lineEndingStyle)
        {
            SshOptions = new ObservableCollection<SshOptionViewModel>();

            _lineEndingStyle = lineEndingStyle;
        }

        private SshConnectionInfoViewModel(SshConnectionInfoViewModel original)
        {
            _host = original._host;
            _sshPort = original._sshPort;
            _username = original._username;
            _identityFile = original._identityFile;
            _useMosh = original._useMosh;
            _moshPortFrom = original._moshPortFrom;
            _moshPortTo = original._moshPortTo;
            _lineEndingStyle = original._lineEndingStyle;
            SshOptions = new ObservableCollection<SshOptionViewModel>(original.SshOptions.Select(o => o.Clone()));
        }

        #endregion Constructors

        #region Methods

        public void FillShellProfile(ShellProfile profile)
        {
            SshConnectionInfoValidationResult result = Validate();

            if (result != SshConnectionInfoValidationResult.Valid)
                throw new Exception("Invalid SSH info.");

            profile.Arguments = GetArgumentsString();
            profile.Location = _useMosh ? MoshExe : SshExeLocationLazy.Value;
            profile.LineEndingTranslation = _lineEndingStyle;

        }

        public SshConnectionInfoValidationResult Validate(bool allowNoUser = false)
        {
            SshConnectionInfoValidationResult result = SshConnectionInfoValidationResult.Valid;

            if (!allowNoUser && string.IsNullOrEmpty(_username))
            {
                result |= SshConnectionInfoValidationResult.UsernameEmpty;
            }

            if (string.IsNullOrEmpty(_host))
            {
                result |= SshConnectionInfoValidationResult.HostEmpty;
            }

            if (_sshPort < 1)
            {
                result |= SshConnectionInfoValidationResult.SshPortZeroOrNegative;
            }

            if (!_useMosh) {
                return result;
            }

            if (_moshPortFrom < 1)
            {
                result |= SshConnectionInfoValidationResult.MoshPortZeroOrNegative;
            }

            if (_moshPortFrom > _moshPortTo)
            {
                result |= SshConnectionInfoValidationResult.MoshPortRangeInvalid;
            }

            return result;
        }

        public ISshConnectionInfo Clone() => new SshConnectionInfoViewModel(this);

        private string GetArgumentsString()
        {
            StringBuilder sb = new StringBuilder();

            if (_sshPort != DefaultSshPort)
                sb.Append($"-p {_sshPort:#####} ");

            if (!string.IsNullOrEmpty(_identityFile))
                sb.Append($"-i \"{_identityFile}\" ");

            foreach (SshOptionViewModel option in SshOptions)
                sb.Append($"-o \"{option.Name}={option.Value}\" ");

            sb.Append($"{_username}@{_host}");

            if (_useMosh)
                sb.Append($" {_moshPortFrom}:{_moshPortTo}");

            return sb.ToString();
        }

        #endregion Methods
    }
}