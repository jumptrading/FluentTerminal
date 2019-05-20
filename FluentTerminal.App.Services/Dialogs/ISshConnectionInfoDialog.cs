using System.Threading.Tasks;
using FluentTerminal.Models;

namespace FluentTerminal.App.Services.Dialogs
{
    public interface ISshConnectionInfoDialog
    {
        Task<ShellProfile> FillSshShellProfileAsync(ShellProfile profile, ISshConnectionInfo input = null);
    }
}