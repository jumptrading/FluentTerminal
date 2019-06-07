using System;
using System.Threading.Tasks;

namespace FluentTerminal.App.Services
{
    public interface IUpdateService
    {
        Task CheckForUpdate(bool runUpdate = false);
        Version GetCurrentVersion();
        Task<Version> GetLatestVersionAsync();
    }
}
