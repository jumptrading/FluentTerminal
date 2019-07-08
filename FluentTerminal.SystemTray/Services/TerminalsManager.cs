using FluentTerminal.App.Services;
using FluentTerminal.Models;
using FluentTerminal.Models.Enums;
using FluentTerminal.Models.Requests;
using FluentTerminal.Models.Responses;
using FluentTerminal.SystemTray.Services.ConPty;
using FluentTerminal.SystemTray.Services.WinPty;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;

namespace FluentTerminal.SystemTray.Services
{
    public struct TerminalSessionInfo
    {
        public DateTime StartTime { get; set; }
        public string ProfileName { get; set; }
        public ITerminalSession Session { get; set; }
    }

    public class TerminalsManager
    {
        private readonly Dictionary<byte, TerminalSessionInfo> _terminals = new Dictionary<byte, TerminalSessionInfo>();

        public event EventHandler<TerminalOutput> DisplayOutputRequested;

        public event EventHandler<TerminalExitStatus> TerminalExited;

        private readonly ISettingsService _settingsService;

        private static readonly Regex EscapeSequencePattern = new Regex(@"((\x9B|\x1B\[)[0-?]*[ -\/]*[@-~])|((\x9D|\x1B\]).*\x07)", RegexOptions.Compiled);

        public TerminalsManager(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public void DisplayTerminalOutput(byte terminalId, byte[] output)
        {
            if (_settingsService.GetApplicationSettings().EnableLogging && Directory.Exists(_settingsService.GetApplicationSettings().LogDirectoryPath))
            {
                var logOutput = output;
                if (_settingsService.GetApplicationSettings().PrintableOutputOnly)
                {
                    string strOutput = System.Text.Encoding.UTF8.GetString(logOutput);
                    strOutput = EscapeSequencePattern.Replace(strOutput, "");
                    logOutput = Encoding.UTF8.GetBytes(strOutput);
                }

                var logFileStream = System.IO.File.Open(_settingsService.GetApplicationSettings().LogDirectoryPath + @"\" + 
                    _terminals[terminalId].StartTime.ToString("yyyyMMddhhmmssfff") + "_" + _terminals[terminalId].ProfileName + ".log", System.IO.FileMode.Append);
                logFileStream.Write(logOutput, 0, logOutput.Length);
                logFileStream.Close();
            }

            DisplayOutputRequested?.Invoke(this, new TerminalOutput
            {
                TerminalId = terminalId,
                Data = output
            });
        }

        public CreateTerminalResponse CreateTerminal(CreateTerminalRequest request)
        {
            if (_terminals.ContainsKey(request.Id))
            {
                // App terminated without cleaning up, removing orphaned sessions
                foreach (var item in _terminals.Values)
                {
                    item.Session.Dispose();
                }
                _terminals.Clear();
            }

            request.Profile.Location = Utilities.ResolveLocation(request.Profile.Location);

            ITerminalSession terminal = null;
            try
            {
                if (request.SessionType == SessionType.WinPty)
                {
                    terminal = new WinPtySession();
                }
                else if (request.SessionType == SessionType.ConPty)
                {
                    terminal = new ConPtySession();
                }
                terminal.Start(request, this);
            }
            catch (Exception e)
            {
                return new CreateTerminalResponse { Error = e.ToString() };
            }

            terminal.ConnectionClosed += OnTerminalConnectionClosed;
            _terminals.Add(terminal.Id, new TerminalSessionInfo { ProfileName = String.IsNullOrEmpty(request.Profile.Name) ? terminal.ShellExecutableName : request.Profile.Name, StartTime = DateTime.Now, Session = terminal });
            return new CreateTerminalResponse
            {
                Success = true,
                ShellExecutableName = terminal.ShellExecutableName
            };
        }

        public void Write(byte id, byte[] data)
        {
            if (_terminals.TryGetValue(id, out TerminalSessionInfo sessionInfo))
            {
                sessionInfo.Session.Write(data);
            }
        }

        public void ResizeTerminal(byte id, TerminalSize size)
        {
            if (_terminals.TryGetValue(id, out TerminalSessionInfo sessionInfo))
            {
                sessionInfo.Session.Resize(size);
            }
            else
            {
                Debug.WriteLine($"ResizeTerminal: terminal with id '{id}' was not found");
            }
        }

        public void CloseTerminal(byte id)
        {
            if (_terminals.TryGetValue(id, out TerminalSessionInfo sessionInfo))
            {
                _terminals.Remove(sessionInfo.Session.Id);
                sessionInfo.Session.Close();
            }
        }

        public string GetDefaultEnvironmentVariableString(Dictionary<string, string> additionalVariables)
        {
            var environmentVariables = Environment.GetEnvironmentVariables();
            environmentVariables["TERM"] = "xterm-256color";
            environmentVariables["TERM_PROGRAM"] = "FluentTerminal";
            environmentVariables["TERM_PROGRAM_VERSION"] = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";

            if (additionalVariables != null)
            {
                foreach (var kvp in additionalVariables)
                {
                    environmentVariables[kvp.Key] = kvp.Value;
                }
            }

            var builder = new StringBuilder();

            foreach (DictionaryEntry item in environmentVariables)
            {
                builder.Append(item.Key).Append("=").Append(item.Value).Append("\0");
            }
            builder.Append('\0');

            return builder.ToString();
        }

        private void OnTerminalConnectionClosed(object sender, int exitcode)
        {
            if (sender is ITerminalSession terminal)
            {
                _terminals.Remove(terminal.Id);
                terminal.Dispose();
                TerminalExited?.Invoke(this, new TerminalExitStatus(terminal.Id, exitcode));
            }
        }
    }
}
