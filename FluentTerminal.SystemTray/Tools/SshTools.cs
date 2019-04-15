using System;
using System.IO;
using System.Text.RegularExpressions;
using FluentTerminal.App.Services.Dialogs;
using Renci.SshNet;

namespace FluentTerminal.SystemTray.Tools
{
    public class SshTools
    {

        private static readonly Regex MoshServerResponseRx =
            new Regex(@"^\s*MOSH\sCONNECT\s(?<port>\d{1,5}) (?<key>\S+)\s*$", RegexOptions.Compiled);

        internal static (string, ushort) GetMoshKeyAndPort(ISshConnectionInfo sshConnectionInfo)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(sshConnectionInfo.Host, sshConnectionInfo.SshPort,
                sshConnectionInfo.Username,
                new PasswordAuthenticationMethod(sshConnectionInfo.Username, sshConnectionInfo.Password));

            string result;

            using (SshClient client = new SshClient(connectionInfo))
            {
                client.Connect();

                SshCommand command = client.CreateCommand("mosh-server new -p " + sshConnectionInfo.MoshPorts);

                result = command.Execute();
            }

            Match match = MoshServerResponseRx.Match(result);

            return match.Success ? (match.Groups["key"].Value, ushort.Parse(match.Groups["port"].Value)) : (null, 0);
        }

        internal static (string, bool) GetIdentityFile(ConnectionInfo connectionInfo)
        {
            DirectoryInfo ssh = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal)).Parent;

            ssh = new DirectoryInfo(Path.Combine(ssh.FullName, ".ssh"));

            FileInfo[] files = ssh.GetFiles();

            return ("", false);
        }

        private static bool TryConnect(ConnectionInfo connectionInfo)
        {
            using (SshClient client = new SshClient(connectionInfo))
            {
                client.Connect();

                return client.IsConnected;
            }

        }
    }
}