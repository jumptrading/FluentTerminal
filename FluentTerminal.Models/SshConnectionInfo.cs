namespace FluentTerminal.Models
{
    public class SshConnectionInfo : ISshConnectionInfo
    {
        public string Host { get; set; }
        public ushort SshPort { get; set; }
        public string Username { get; set; }
        public string IdentityFile { get; set; }
        public bool UseMosh { get; set; }
        public string Password { get; set; }
        public string Passphrase { get; set; }
        public string MoshPorts { get; set; }
    }
}