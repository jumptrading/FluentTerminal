using System.Text.RegularExpressions;
using FluentTerminal.App.Services.Dialogs;
using Renci.SshNet;

namespace FluentTerminal.App.ViewModels.Utilities
{
    public static class MoshAuthenticator
    {
        private static readonly Regex MoshServerResponseRx =
            new Regex(@"^\s*MOSH\sCONNECT\s(?<port>\d{1,5}) (?<key>\S+)\s*$", RegexOptions.Compiled);

        public static (string, ushort) GetMoshKeyAndPort(IMoshConnectionInfo moshConnectionInfo)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(moshConnectionInfo.Host, moshConnectionInfo.Port,
                moshConnectionInfo.Username,
                new PasswordAuthenticationMethod(moshConnectionInfo.Username, moshConnectionInfo.Password));

            string result;

            using (SshClient client = new SshClient(connectionInfo))
            {
                client.Connect();

                SshCommand command = client.CreateCommand("mosh-server new -p " + moshConnectionInfo.MoshPorts);

                result = command.Execute();
            }

            Match match = MoshServerResponseRx.Match(result);

            return match.Success ? (match.Groups["key"].Value, ushort.Parse(match.Groups["port"].Value)) : (null, 0);
        }
    }
}