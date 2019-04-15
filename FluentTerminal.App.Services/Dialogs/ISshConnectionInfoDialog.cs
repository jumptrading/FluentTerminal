using System.Threading.Tasks;
using FluentTerminal.Models;

namespace FluentTerminal.App.Services.Dialogs
{
    public interface ISshConnectionInfoDialog
    {
        Task<SshConnectionInfo> GetSshConnectionInfoAsync();
    }
}