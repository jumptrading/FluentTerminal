using System.Threading.Tasks;

namespace FluentTerminal.App.Services.Dialogs
{
    public interface IMoshConnectionInfoDialog
    {
        Task<IMoshConnectionInfo> GetMoshConnectionInfoAsync();
    }
}