namespace FluentTerminal.Models.Requests
{
    public class TerminalExitedRequest
    {
        public int TerminalId { get; set; }
        public int ExitCode { get; set; }
    }
}