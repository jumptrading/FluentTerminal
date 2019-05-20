﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Windows.UI.Popups;
using FluentTerminal.App.Services.Utilities;
using FluentTerminal.App.ViewModels;
using FluentTerminal.Models;
using FluentTerminal.Models.Enums;

namespace FluentTerminal.App.Services
{
    // Resources:
    // https://tools.ietf.org/html/draft-ietf-secsh-scp-sftp-ssh-uri-04#section-3
    // https://man.openbsd.org/ssh
    public class SshHelperService : ISshHelperService
    {
        #region Constants

        private const string SshUriScheme = "ssh";
        private const string MoshUriScheme = "mosh";

        // Constant derived from https://man.openbsd.org/ssh
        private const string IdentityFileOptionName = "IdentityFile";

        private static readonly string[] ValidMoshPortsNames = { "mosh_ports", "mosh-ports" };

        private static readonly Regex MoshRangeRx =
            new Regex(@"^(?<from>\d{1,5})[:-](?<to>\d{1,5})$", RegexOptions.Compiled);

        #endregion Constants

        #region Static

        private static IEnumerable<string> GetErrorMessages(SshConnectionInfoValidationResult result)
        {
            foreach (SshConnectionInfoValidationResult error in Enum
                .GetValues(typeof(SshConnectionInfoValidationResult)).Cast<SshConnectionInfoValidationResult>()
                .Where(v => v != SshConnectionInfoValidationResult.Valid))
                if ((error & result) == error)
                    yield return I18N.Translate($"{nameof(SshConnectionInfoValidationResult)}.{error}");
        }

        private static void LoadSshOptionsFromUri(SshConnectionInfoViewModel vm, string optsString)
        {
            vm.SshOptions.Clear();

            if (string.IsNullOrEmpty(optsString))
            {
                return;
            }

            foreach (SshOptionViewModel option in ParseSshOptionsFromUri(optsString, ','))
            {
                if (option.Name.Equals(IdentityFileOptionName, StringComparison.OrdinalIgnoreCase))
                {
                    vm.IdentityFile = option.Value;
                }
                else if (vm.SshOptions.Any(opt => string.Equals(opt.Name, option.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new FormatException($"SSH option '{option.Name}' is defined more than once.");
                }
                else
                {
                    vm.SshOptions.Add(option);
                }
            }
        }

        private static IEnumerable<SshOptionViewModel> ParseSshOptionsFromUri(string uriOpts, char separator) =>
            uriOpts.Split(separator).Select(ParseSshOptionFromUri);

        private static SshOptionViewModel ParseSshOptionFromUri(string option)
        {
            string[] nv = option.Split('=');

            if (nv.Length != 2 || string.IsNullOrEmpty(nv[0]))
            {
                throw new FormatException($"Invalid SSH option '{option}'.");
            }
            return new SshOptionViewModel { Name = HttpUtility.UrlDecode(nv[0]), Value = HttpUtility.UrlDecode(nv[1]) };
        }
        
        private static string ToUriOption(SshOptionViewModel option)
        {
            string name = option.Name;

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(SshOptionViewModel.Name)} must contain non empty string.");
            }

            name = HttpUtility.UrlEncode(name);

            string value = option.Value;
            if (!string.IsNullOrEmpty(value))
                value = HttpUtility.UrlEncode(value);

            return $"{name}={value}";
        }

        #endregion Static

        #region Fields

        private readonly IDialogService _dialogService;
        private readonly IDefaultValueProvider _defaultValueProvider;

        #endregion Fields

        #region Constructor

        public SshHelperService(IDialogService dialogService, IDefaultValueProvider defaultValueProvider)
        {
            _dialogService = dialogService;
            _defaultValueProvider = defaultValueProvider;
        }

        #endregion Constructor

        #region Methods

        public bool IsSsh(Uri uri) =>
            SshUriScheme.Equals(uri?.Scheme, StringComparison.OrdinalIgnoreCase) ||
            MoshUriScheme.Equals(uri?.Scheme, StringComparison.OrdinalIgnoreCase);

        public async Task<ShellProfile> FillSshShellProfileAsync(ShellProfile profile, Uri uri)
        {
            if (!IsSsh(uri))
                throw new ArgumentException("Input argument is not a SSH URI.", nameof(uri));

            SshConnectionInfoViewModel sshConnectionInfo;

            try
            {
                sshConnectionInfo = ParseSsh(uri);
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Invalid link: {ex.Message}", "Invalid Link").ShowAsync();

                return null;
            }

            var validationResult = sshConnectionInfo.Validate();

            if (validationResult != SshConnectionInfoValidationResult.Valid)
                // Happens if the link doesn't contain all the needed data, so we have to prompt user to complete.
                profile = await _dialogService.ShowSshConnectionInfoDialogAsync(profile, sshConnectionInfo);

            return profile;
        }

        public string ConvertToUri(ISshConnectionInfo sshConnectionInfo)
        {
            var validationResult = sshConnectionInfo.Validate(true);
            if (validationResult != SshConnectionInfoValidationResult.Valid)
                throw new ArgumentException(string.Join(", ", GetErrorMessages(validationResult)),
                    nameof(sshConnectionInfo));

            SshConnectionInfoViewModel sshConnectionInfoVm = (SshConnectionInfoViewModel) sshConnectionInfo;

            StringBuilder sb = new StringBuilder(sshConnectionInfoVm.UseMosh ? MoshUriScheme : SshUriScheme);

            sb.Append("://");

            bool containsUserInfo = false;

            if (!string.IsNullOrEmpty(sshConnectionInfoVm.Username))
            {
                sb.Append(HttpUtility.UrlEncode(sshConnectionInfoVm.Username));
                containsUserInfo = true;
            }

            if (!string.IsNullOrEmpty(sshConnectionInfoVm.IdentityFile) || sshConnectionInfoVm.SshOptions.Any())
            {
                bool first = true;

                sb.Append(";");

                if (!string.IsNullOrEmpty(sshConnectionInfoVm.IdentityFile))
                {
                    sb.Append($"{IdentityFileOptionName}={HttpUtility.UrlEncode(sshConnectionInfoVm.IdentityFile)}");

                    first = false;
                }

                foreach (string option in sshConnectionInfoVm.SshOptions.Select(ToUriOption))
                {
                    if (!first)
                        sb.Append(",");
                    sb.Append(option);
                    first = false;
                }

                containsUserInfo = true;
            }

            if (containsUserInfo)
            {
                sb.Append("@");
            }

            sb.Append(sshConnectionInfoVm.Host);

            if (sshConnectionInfoVm.SshPort != SshConnectionInfoViewModel.DefaultSshPort)
            {
                sb.Append(":");
                sb.Append(sshConnectionInfoVm.SshPort.ToString("#####"));
            }

            if (sshConnectionInfoVm.UseMosh)
            {
                sb.Append("?");
                sb.Append(ValidMoshPortsNames[0]);
                sb.Append("=");
                sb.Append(sshConnectionInfoVm.MoshPortFrom.ToString("#####"));
                sb.Append("-");
                sb.Append(sshConnectionInfoVm.MoshPortTo.ToString("#####"));
            }

            return sb.ToString();
        }

        public string GetErrorMessage(SshConnectionInfoValidationResult result) =>
            string.Join(Environment.NewLine, GetErrorMessages(result));

        private SshConnectionInfoViewModel ParseSsh(Uri uri)
        {
            SshConnectionInfoViewModel vm =
                new SshConnectionInfoViewModel(_defaultValueProvider.DefaultSshLineEndingStyle)
                {
                    Host = uri.Host, UseMosh = MoshUriScheme.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase)
                };


            if (uri.Port >= 0)
            {
                vm.SshPort = (ushort)uri.Port;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string[] parts = uri.UserInfo.Split(';');

                if (parts.Length > 2)
                {
                    throw new FormatException($"UserInfo part contains {parts.Length} elements.");
                }

                vm.Username = HttpUtility.UrlDecode(parts[0]);

                if (parts.Length > 1)
                {
                    LoadSshOptionsFromUri(vm, parts[1]);
                }
            }

            if (string.IsNullOrEmpty(uri.Query))
                return vm;

            if (!vm.UseMosh)
            {
                throw new FormatException("Query parameters are not supported in SSH links.");
            }

            string queryString = uri.Query;

            if (queryString.StartsWith("?", StringComparison.Ordinal))
            {
                queryString = queryString.Substring(1);
            }

            if (string.IsNullOrEmpty(queryString))
            {
                return vm;
            }

            foreach (SshOptionViewModel option in ParseSshOptionsFromUri(queryString, '&'))
            {
                if (ValidMoshPortsNames.Any(n => n.Equals(option.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    Match match = MoshRangeRx.Match(option.Value);
                    if (!match.Success)
                    {
                        throw new FormatException($"Invalid mosh ports range '{option.Value}'.");
                    }

                    vm.MoshPortFrom = ushort.Parse(match.Groups["from"].Value);
                    vm.MoshPortTo = ushort.Parse(match.Groups["to"].Value);
                }
                else
                {
                    throw new FormatException($"Unknown query parameter '{option.Name}'.");
                }
            }

            return vm;
        }

        #endregion Methods
    }
}