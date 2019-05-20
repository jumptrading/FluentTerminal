using System;
using System.Threading.Tasks;
using FluentTerminal.Models;
using FluentTerminal.Models.Enums;

namespace FluentTerminal.App.Services
{
    public interface ISshHelperService
    {
        bool IsSsh(Uri uri);

        Task<ShellProfile> FillSshShellProfileAsync(ShellProfile profile, Uri uri);

        string ConvertToUri(ISshConnectionInfo sshConnectionInfo);

        string GetErrorMessage(SshConnectionInfoValidationResult result);
    }
}