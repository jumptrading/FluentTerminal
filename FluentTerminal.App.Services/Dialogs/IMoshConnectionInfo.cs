namespace FluentTerminal.App.Services.Dialogs
{
    public interface IMoshConnectionInfo : ISshConnectionInfo
    {
        string MoshPorts { get; set; }

        string Password { get; set; }
    }
}