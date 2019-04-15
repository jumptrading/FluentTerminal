using System.Threading.Tasks;
using FluentTerminal.Models;

namespace FluentTerminal.App.Services.Dialogs
{
    public interface ISshAuthenticationDialog
    {
        Task<SshConnectionInfo> GetSshAuthenticationAsync(ISshConnectionInfo sshConnectionInfo);
    }
}