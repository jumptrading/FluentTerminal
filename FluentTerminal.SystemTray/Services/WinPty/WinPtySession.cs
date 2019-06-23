using FluentTerminal.Models;
using FluentTerminal.Models.Requests;
using FluentTerminal.SystemTray.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static winpty.WinPty;

namespace FluentTerminal.SystemTray.Services.WinPty
{
    public class WinPtySession : IDisposable, ITerminalSession
    {
        private bool _disposedValue;
        private IntPtr _handle;
        private Stream _stdin;
        private Stream _stdout;
        private TerminalsManager _terminalsManager;
        private Process _shellProcess;
        private bool _exited;
        private TerminalSize _terminalSize;

        public void Start(CreateTerminalRequest request, TerminalsManager terminalsManager)
        {
            Id = request.Id;
            _terminalsManager = terminalsManager;
            _terminalSize = request.Size;

            var configHandle = IntPtr.Zero;
            var spawnConfigHandle = IntPtr.Zero;
            var errorHandle = IntPtr.Zero;

            try
            {
                configHandle = winpty_config_new(WINPTY_FLAG_COLOR_ESCAPES, out errorHandle);
                winpty_config_set_initial_size(configHandle, request.Size.Columns, request.Size.Rows);

                _handle = winpty_open(configHandle, out errorHandle);
                if (errorHandle != IntPtr.Zero)
                {
                    throw new Exception(winpty_error_msg(errorHandle));
                }

                var cwd = GetWorkingDirectory(request.Profile);
                var args = request.Profile.Arguments;

                if (!string.IsNullOrWhiteSpace(request.Profile.Location))
                {
                    args = $"\"{request.Profile.Location}\" {args}";
                }

                spawnConfigHandle = winpty_spawn_config_new(WINPTY_SPAWN_FLAG_AUTO_SHUTDOWN, request.Profile.Location, args, cwd, terminalsManager.GetDefaultEnvironmentVariableString(request.Profile.EnvironmentVariables), out errorHandle);
                if (errorHandle != IntPtr.Zero)
                {
                    throw new Exception(winpty_error_msg(errorHandle));
                }

                _stdin = CreatePipe(winpty_conin_name(_handle), PipeDirection.Out);
                _stdout = CreatePipe(winpty_conout_name(_handle), PipeDirection.In);

                if (!winpty_spawn(_handle, spawnConfigHandle, out IntPtr process, out IntPtr thread, out int procError, out errorHandle))
                {
                    throw new Exception($@"Failed to start the shell process. Please check your shell settings.{Environment.NewLine}Tried to start: {request.Profile.Location} ""{request.Profile.Arguments}""");
                }

                var shellProcessId = ProcessApi.GetProcessId(process);
                _shellProcess = Process.GetProcessById(shellProcessId);
                _shellProcess.EnableRaisingEvents = true;
                _shellProcess.Exited += _shellProcess_Exited;

                if (!string.IsNullOrWhiteSpace(request.Profile.Location))
                {
                    ShellExecutableName = Path.GetFileNameWithoutExtension(request.Profile.Location);
                }
                else
                {
                    ShellExecutableName = request.Profile.Arguments.Split(' ')[0];
                }
            }
            finally
            {
                winpty_config_free(configHandle);
                winpty_spawn_config_free(spawnConfigHandle);
                winpty_error_free(errorHandle);
            }

            ListenToStdOut();
        }

        private void _shellProcess_Exited(object sender, EventArgs e)
        {
            Close();
            _exited = true;
        }

        ~WinPtySession()
        {
            Dispose(false);
        }

        

        private string GetWorkingDirectory(ShellProfile configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.WorkingDirectory) || !Directory.Exists(configuration.WorkingDirectory))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            return configuration.WorkingDirectory;
        }

        public event EventHandler<int> ConnectionClosed;

        public byte Id { get; private set; }

        public string ShellExecutableName { get; private set; }

        public void Dispose()
        {
            _shellProcess.Exited -= _shellProcess_Exited;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            int exitCode = -1;
            if (_shellProcess != null && _shellProcess.HasExited)
            {
                exitCode = _shellProcess.ExitCode;
            }
            ConnectionClosed?.Invoke(this, exitCode);
        }

        public void Write(byte[] data)
        {
            _stdin.Write(data, 0, data.Length);
        }

        public void Resize(TerminalSize size)
        {
            var errorHandle = IntPtr.Zero;
            try
            {
                winpty_set_size(_handle, size.Columns, size.Rows, out errorHandle);
                if (errorHandle != IntPtr.Zero)
                {
                    throw new Exception(winpty_error_msg(errorHandle));
                }
                _terminalSize = size;
            }
            finally
            {
                winpty_error_free(errorHandle);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _stdin?.Dispose();
                    _stdout?.Dispose();
                }

                winpty_free(_handle);

                _disposedValue = true;
            }
        }

        private Stream CreatePipe(string pipeName, PipeDirection direction)
        {
            string serverName = ".";
            if (pipeName.StartsWith("\\"))
            {
                int slash3 = pipeName.IndexOf('\\', 2);
                if (slash3 != -1)
                {
                    serverName = pipeName.Substring(2, slash3 - 2);
                }
                int slash4 = pipeName.IndexOf('\\', slash3 + 1);
                if (slash4 != -1)
                {
                    pipeName = pipeName.Substring(slash4 + 1);
                }
            }

            var pipe = new NamedPipeClientStream(serverName, pipeName, direction);
            pipe.Connect();
            return pipe;
        }

        private void ListenToStdOut()
        {
            Task.Factory.StartNew(async () =>
            {
                using (var reader = new StreamReader(_stdout))
                {
                    do
                    {
                        var buffer = new byte[Math.Max(1024, _terminalSize.Columns * _terminalSize.Rows * 4)];
                        var readBytes = await _stdout.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        var read = new byte[readBytes];
                        Buffer.BlockCopy(buffer, 0, read, 0, readBytes);

                        if (readBytes > 0)
                        {
                            _terminalsManager.DisplayTerminalOutput(Id, read);
                        }
                    }
                    while (!_exited);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void Mute(bool value)
        {
            //Process cmdProcess = Process.Start("cmd", "/k \x07");

            Process cmdProcess = new Process();
            cmdProcess.StartInfo.FileName = "cmd.exe";
            cmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmdProcess.StartInfo.Arguments = "/k \x07";
            cmdProcess.Start();

            int conhostId = 0;
            foreach (Process conhost in Process.GetProcessesByName("conhost"))
            {
                try
                {
                    Process parent = ParentProcessUtilities.GetParentProcess(conhost.Id);
                    if (parent != null && cmdProcess.Id == parent.Id)
                    {
                        conhostId = conhost.Id;
                    }
                }
                catch (Exception ex)
                {

                }
            }
            Console.WriteLine($"conhostProcess = {conhostId}");
            VolumeMixer.SetApplicationMute(conhostId, value);

            cmdProcess.Kill();

            /*Process winptyAgentProcess = ParentProcessUtilities.GetParentProcess(_shellProcess.Id);
            Console.WriteLine($"winptyAgentProcess = {winptyAgentProcess.Id}");

            int conhostId = 0;
            foreach (Process conhost in Process.GetProcessesByName("conhost"))
            {
                try
                {
                    Process parent = ParentProcessUtilities.GetParentProcess(conhost.Id);
                    if (parent != null && winptyAgentProcess.Id == parent.Id)
                    {
                        conhostId = conhost.Id;
                    }
                }
                catch (Exception ex)
                {

                }
            }
            Console.WriteLine($"conhostProcess = {conhostId}");
            VolumeMixer.SetApplicationMute(conhostId, value);*/
        }

        /// <summary>
        /// A utility class to determine a process parent.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ParentProcessUtilities
        {
            // These members must match PROCESS_BASIC_INFORMATION
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;

            [DllImport("ntdll.dll")]
            private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

            /// <summary>
            /// Gets the parent process of the current process.
            /// </summary>
            /// <returns>An instance of the Process class.</returns>
            public static Process GetParentProcess()
            {
                return GetParentProcess(Process.GetCurrentProcess().Handle);
            }

            /// <summary>
            /// Gets the parent process of specified process.
            /// </summary>
            /// <param name="id">The process id.</param>
            /// <returns>An instance of the Process class.</returns>
            public static Process GetParentProcess(int id)
            {
                Process process = Process.GetProcessById(id);
                return GetParentProcess(process.Handle);
            }

            /// <summary>
            /// Gets the parent process of a specified process.
            /// </summary>
            /// <param name="handle">The process handle.</param>
            /// <returns>An instance of the Process class.</returns>
            public static Process GetParentProcess(IntPtr handle)
            {
                ParentProcessUtilities pbi = new ParentProcessUtilities();
                int returnLength;
                int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
                if (status != 0)
                    throw new Win32Exception(status);

                try
                {
                    return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
                }
                catch (ArgumentException)
                {
                    // not found
                    return null;
                }
            }
        }
    }

    public class VolumeMixer
    {
        public static float? GetApplicationVolume(int pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            float level;
            volume.GetMasterVolume(out level);
            Marshal.ReleaseComObject(volume);
            return level * 100;
        }

        public static bool? GetApplicationMute(int pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            bool mute;
            volume.GetMute(out mute);
            Marshal.ReleaseComObject(volume);
            return mute;
        }

        public static void SetApplicationVolume(int pid, float level)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMasterVolume(level / 100, ref guid);
            Marshal.ReleaseComObject(volume);
        }

        public static void SetApplicationMute(int pid, bool mute)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMute(mute, ref guid);
            Marshal.ReleaseComObject(volume);
        }

        private static ISimpleAudioVolume GetVolumeObject(int pid)
        {
            // get the speakers (1st render + multimedia) device
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            // activate the session manager. we need the enumerator
            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            // search for an audio session with the required name
            // NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
            ISimpleAudioVolume volumeControl = null;
            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl2 ctl;
                sessionEnumerator.GetSession(i, out ctl);
                int cpid;
                ctl.GetProcessId(out cpid);

                if (cpid == pid)
                {
                    volumeControl = ctl as ISimpleAudioVolume;
                    break;
                }
                Marshal.ReleaseComObject(ctl);
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
            return volumeControl;
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int NotImpl1();

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

        // the rest is not implemented
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        // the rest is not implemented
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int NotImpl1();
        int NotImpl2();

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        // the rest is not implemented
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl2 Session);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float fLevel, ref Guid EventContext);

        [PreserveSig]
        int GetMasterVolume(out float pfLevel);

        [PreserveSig]
        int SetMute(bool bMute, ref Guid EventContext);

        [PreserveSig]
        int GetMute(out bool pbMute);
    }

    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl2
    {
        // IAudioSessionControl
        [PreserveSig]
        int NotImpl0();

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid pRetVal);

        [PreserveSig]
        int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int NotImpl1();

        [PreserveSig]
        int NotImpl2();

        // IAudioSessionControl2
        [PreserveSig]
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetProcessId(out int pRetVal);

        [PreserveSig]
        int IsSystemSoundsSession();

        [PreserveSig]
        int SetDuckingPreference(bool optOut);
    }
}