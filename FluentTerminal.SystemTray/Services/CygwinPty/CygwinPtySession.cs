using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CygwinPtyClr;
using FluentTerminal.Models;
using FluentTerminal.Models.Requests;

namespace FluentTerminal.SystemTray.Services.CygwinPty
{
    public class CygwinPtySession : ITerminalSession
    {
        private readonly object _lock = new object();
        private readonly CygwinPtyClr.CygwinPty _terminal = new CygwinPtyClr.CygwinPty();

        private TerminalsManager _terminalsManager;
        private bool _paused;
        private bool _exited;
        private Queue<byte[]> _buffer;

        public byte Id { get; private set; }

        public string ShellExecutableName { get; private set; }

        public event EventHandler<int> ConnectionClosed;

        private void TerminalOutputReceived(object sender, DataEventArgs e)
        {
            lock (_lock)
            {
                if (_exited)
                    return;

                if (_paused)
                {
                    if (_buffer == null)
                        _buffer = new Queue<byte[]>();

                    _buffer.Enqueue(e.Data);

                    return;
                }
            }

            _terminalsManager?.DisplayTerminalOutput(Id, e.Data);
        }

        private void TerminalCorrupted(object sender, EventArgs e) => Close();

        public void Dispose()
        {
            Close();

            _terminal.Dispose();
        }

        public void Close()
        {
            lock (_lock)
            {
                if (_exited)
                    return;

                _exited = true;
            }

            ConnectionClosed?.Invoke(this, -1);
        }

        public void Resize(TerminalSize size)
        {
            _terminal?.SetWinSizeAsync(new WinSize {Height = (ushort) size.Rows, Width = (ushort) size.Columns}).Wait();
        }

        public void Write(byte[] data)
        {
            _terminal.WriteInputAsync(data).Wait();
        }

        public void Start(CreateTerminalRequest request, TerminalsManager terminalsManager)
        {
            _terminalsManager = terminalsManager;
            Id = request.Id;

            ShellExecutableName = Path.GetFileNameWithoutExtension(request.Profile.Location);

            _terminal.Corrupted += TerminalCorrupted;
            _terminal.OutputReceived += TerminalOutputReceived;

            _terminal.SpawnAsync(request.Profile.Location, request.Profile.Arguments, (ushort) request.Size.Columns,
                    (ushort) request.Size.Rows, request.Profile.EnvironmentVariables, request.Profile.WorkingDirectory)
                .Wait();
        }

        public void Pause(bool value)
        {
            lock (_lock)
            {
                if (_exited || value == _paused)
                    return;

                if (_paused && _buffer != null)
                {
                    // was paused
                    while (_buffer.Any())
                    {
                        _terminalsManager?.DisplayTerminalOutput(Id, _buffer.Dequeue());
                    }

                    _buffer = null;
                }

                _paused = value;
            }
        }
    }
}