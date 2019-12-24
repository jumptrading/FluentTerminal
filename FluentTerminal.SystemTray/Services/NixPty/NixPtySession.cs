using System;
using System.IO;
using System.Threading.Tasks;
using FluentTerminal.Models;
using FluentTerminal.Models.Requests;
using PtyClr;

namespace FluentTerminal.SystemTray.Services.NixPty
{
    public class NixPtySession : ITerminalSession
    {
        private readonly Pty _terminal;

        private TerminalsManager _terminalsManager;
        private bool _paused;
        private bool _exited;

        public byte Id { get; private set; }

        public string ShellExecutableName { get; private set; }

        public event EventHandler<int> ConnectionClosed;

        internal NixPtySession(PtyBuild ptyBuild) => _terminal = new Pty(ptyBuild);

        private void TerminalCorrupt(object sender, EventArgs e) => Close();

        public void Dispose() => Close();

        public void Close()
        {
            if (_exited)
                return;

            _exited = true;

            _terminal.Dispose();

            ConnectionClosed?.Invoke(this, -1);
        }

        public void Resize(TerminalSize size)
        {
            _terminal?.SetWinSizeAsync(new WinSize {Height = (ushort) size.Rows, Width = (ushort) size.Columns}).Wait();
        }

        public void Write(byte[] data)
        {
            _terminal.WriteInput(data);
        }

        public void Start(CreateTerminalRequest request, TerminalsManager terminalsManager)
        {
            _terminalsManager = terminalsManager;
            Id = request.Id;

            ShellExecutableName = Path.GetFileNameWithoutExtension(request.Profile.Location);

            _terminal.Corrupt += TerminalCorrupt;

            _terminal.Spawn(request.Profile.Location, request.Profile.Arguments, (ushort) request.Size.Columns,
                (ushort) request.Size.Rows, request.Profile.EnvironmentVariables, request.Profile.WorkingDirectory,
                LogLevel.Trace, true);

            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = ListenToStdOutAsync();
        }

        public void Pause(bool value)
        {
            _paused = value;
        }

        private const int ReadBufferLength = 4096;

        private async Task ListenToStdOutAsync()
        {
            // Just to release the calling thread asap
            await Task.Delay(10).ConfigureAwait(false);

            var buffer = new byte[ReadBufferLength];

            while (!_exited)
            {
                if (_paused)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                    continue;
                }

                int read;

                try
                {
                    read = await _terminal.OutputStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                }
                catch
                {
                    read = 0;
                }

                if (read < 1)
                {
                    // We'll wait and continue. If the terminal is corrupt _exited will be true anyway.
                    await Task.Delay(10).ConfigureAwait(false);
                    continue;
                }

                var toSend = buffer;
                if (read != ReadBufferLength)
                {
                    toSend = new byte[read];
                    Buffer.BlockCopy(buffer, 0, toSend, 0, read);
                }

                _terminalsManager.DisplayTerminalOutput(Id, toSend);
            }
        }
    }
}