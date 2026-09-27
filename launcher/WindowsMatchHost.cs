using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace SkylineRush
{
    // Trusted local component. No remotely callable process-control endpoint.
    public sealed class WindowsMatchHost : IDisposable
    {
        readonly string root, state;
        readonly FileStream ownership;
        readonly Dictionary<Guid, Match> matches = new Dictionary<Guid, Match>();
        readonly int firstPort, capacity;
        bool disposed;
        public sealed class Match
        {
            public Guid Id { get; internal set; }
            public int Port { get; internal set; }
            public string Password { get; internal set; }
            public string Home { get; internal set; }
            internal Process Process;
            internal IntPtr Job;
            internal DateTime Started;
            public int ProcessId { get { return Process.Id; } }
            public bool HasExited { get { return Process.HasExited; } }
            public bool IsReady
            {
                get
                {
                    if (HasExited) return false;
                    string path = Path.Combine(Home, "server.log");
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            stream.Seek(Math.Max(0, stream.Length - 32768), SeekOrigin.Begin);
                            using (var reader = new StreamReader(stream)) return reader.ReadToEnd().Contains("Dedicated server started");
                        }
                    }
                    catch (IOException) { return false; }
                }
            }
        }
        public WindowsMatchHost(string gameRoot, string stateDirectory, int first = 29801, int slots = 8)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) throw new PlatformNotSupportedException("Windows host required.");
            if (slots < 1 || slots > 64 || first < 1024 || first + 2 * slots - 1 > 65535) throw new ArgumentOutOfRangeException("Invalid server port range.");
            root = Path.GetFullPath(gameRoot); state = Path.GetFullPath(stateDirectory);
            firstPort = first; capacity = slots;
            PrivateDirectory(state);
            ownership = new FileStream(Path.Combine(state, "host.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            // Previous host death closes its job handles, killing its servers. Never kill a PID read from disk.
            foreach (string folder in Directory.GetDirectories(state))
            {
                Guid id;
                if (Guid.TryParse(Path.GetFileName(folder), out id)) File.Delete(Path.Combine(folder, "servinit.cfg"));
            }
        }
        static void PrivateDirectory(string path)
        {
            Directory.CreateDirectory(path);
            var acl = new DirectorySecurity();
            acl.SetAccessRuleProtection(true, false);
            acl.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(path, acl);
        }
        static bool FreePair(int port)
        {
            var sockets = new List<Socket>();
            try
            {
                for (int p = port; p <= port + 1; p++)
                {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sockets.Add(socket); socket.ExclusiveAddressUse = true;
                    socket.Bind(new IPEndPoint(IPAddress.Any, p));
                }
                return true;
            }
            catch (SocketException) { return false; }
            finally { foreach (var socket in sockets) socket.Dispose(); }
        }
        public Match Start(Guid id, string mode, string district)
        {
            if (disposed) throw new ObjectDisposedException("WindowsMatchHost");
            if (id == Guid.Empty || matches.ContainsKey(id)) throw new ArgumentException("Invalid or duplicate room.");
            int players, mutators, enemies;
            switch (mode)
            {
                case "duel": players = 2; mutators = 1; enemies = 0; break;
                case "2v2": players = 4; mutators = 0; enemies = 0; break;
                case "4v4": players = 8; mutators = 0; enemies = 0; break;
                case "coop2": players = 2; mutators = 258; enemies = 12; break;
                case "coop4": players = 4; mutators = 258; enemies = 16; break;
                default: throw new ArgumentException("Unsupported mode.");
            }
            if (district != "heights" && district != "lagoon" && district != "rift") throw new ArgumentException("Unsupported district.");
            Reap(TimeSpan.FromHours(2));
            int port = 0;
            for (int candidate = firstPort; candidate < firstPort + capacity * 2; candidate += 2)
            {
                bool used = false;
                foreach (Match running in matches.Values) if (running.Port == candidate) used = true;
                if (!used && FreePair(candidate)) { port = candidate; break; }
            }
            if (port == 0) throw new InvalidOperationException("No free match ports. Close an existing room and try again.");
            string exe = Path.Combine(root, "bin", "amd64", "redeclipse_server_windows_amd64.exe");
            if (!File.Exists(exe)) throw new FileNotFoundException("Dedicated server missing. Extract the full Windows package.");
            string home = Path.Combine(state, id.ToString()); PrivateDirectory(home);
            File.Delete(Path.Combine(home, "server.log"));
            byte[] random = new byte[18]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(random);
            string password = Convert.ToBase64String(random).Replace('+', '-').Replace('/', '_');
            string cfg = "serverport " + port + "\nserverlanport 0\nhttpserver 0\nservermaster \"\"\nserverpass \"" + password + "\"\n" +
                "sv_serverdesc \"Skyline Rush Alpha\"\nsv_serverclients " + players + "\nsv_serverspectators 0\nsv_resetvarsonend 0\n" +
                "sv_defaultmode 2\nsv_defaultmuts " + mutators + "\nsv_defaultmap \"maps/skyline/" + district + "\"\n" +
                "sv_rotatemode 0\nsv_rotatemuts 0\nsv_rotatemaps 0\nsv_botbalance 0\nsv_botbalanceduel 0\nsv_botbalancesurvivor 0\nsv_botlimit 0\n" +
                "sv_enemylimit " + enemies + "\nsv_enemyspawntime 20000\nsv_enemyspawndelay 3000\nsv_coopskillmin 35\nsv_coopskillmax 55\n" +
                "sv_enemyskillmin 35\nsv_enemyskillmax 55\nsv_timelimit 10\nsv_teambalance 4\n" +
                "sv_teamalphaname \"Jozi Falcons\"\nsv_teamomeganame \"Lagos Comets\"\nsv_teamenemyname \"Rogue Machines\"\n";
            File.WriteAllText(Path.Combine(home, "servinit.cfg"), cfg, Encoding.ASCII);
            IntPtr job = IntPtr.Zero; PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            try
            {
                job = CreateJobObject(IntPtr.Zero, null);
                if (job == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
                var limit = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION(); limit.BasicLimitInformation.LimitFlags = 0x2000; // KILL_ON_JOB_CLOSE
                if (!SetInformationJobObject(job, 9, ref limit, (uint)Marshal.SizeOf(limit))) throw new System.ComponentModel.Win32Exception();
                var si = new STARTUPINFO(); si.cb = Marshal.SizeOf(si);
                var command = new StringBuilder("\"" + exe + "\" -h\"" + home + "\" -gserver.log -ss1 -sm -xskyline_startroom");
                // Suspended start removes the orphan window between process creation and job assignment.
                if (!CreateProcess(exe, command, IntPtr.Zero, IntPtr.Zero, false, 0x4 | 0x08000000, IntPtr.Zero, root, ref si, out pi)) throw new System.ComponentModel.Win32Exception();
                if (!AssignProcessToJobObject(job, pi.hProcess)) throw new System.ComponentModel.Win32Exception();
                var process = Process.GetProcessById((int)pi.dwProcessId);
                if (ResumeThread(pi.hThread) == uint.MaxValue) { process.Dispose(); throw new System.ComponentModel.Win32Exception(); }
                var match = new Match { Id = id, Port = port, Password = password, Home = home, Process = process, Job = job, Started = DateTime.UtcNow };
                matches.Add(id, match); job = IntPtr.Zero;
                return match;
            }
            catch
            {
                if (pi.hProcess != IntPtr.Zero) TerminateProcess(pi.hProcess, 1);
                File.Delete(Path.Combine(home, "servinit.cfg")); throw;
            }
            finally
            {
                if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
                if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
                if (job != IntPtr.Zero) CloseHandle(job);
            }
        }
        public void Stop(Guid id)
        {
            Match match; if (!matches.TryGetValue(id, out match)) return;
            CloseHandle(match.Job); // Server has no graceful local shutdown IPC; terminate this owned job only.
            match.Process.WaitForExit(5000); match.Process.Dispose(); matches.Remove(id);
            File.Delete(Path.Combine(match.Home, "servinit.cfg")); match.Password = null;
        }
        public List<Guid> Reap(TimeSpan maximumLifetime)
        {
            var ended = new List<Guid>();
            foreach (Match match in matches.Values)
                if (match.HasExited || DateTime.UtcNow - match.Started >= maximumLifetime) ended.Add(match.Id);
            foreach (Guid id in ended) Stop(id);
            return ended;
        }
        public void Dispose()
        {
            if (disposed) return;
            foreach (Guid id in new List<Guid>(matches.Keys)) Stop(id);
            ownership.Dispose(); disposed = true;
        }
        [StructLayout(LayoutKind.Sequential)] struct BASIC_LIMIT { public long PerProcessUserTimeLimit, PerJobUserTimeLimit; public uint LimitFlags; public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize; public uint ActiveProcessLimit; public UIntPtr Affinity; public uint PriorityClass, SchedulingClass; }
        [StructLayout(LayoutKind.Sequential)] struct IO_COUNTERS { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }
        [StructLayout(LayoutKind.Sequential)] struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION { public BASIC_LIMIT BasicLimitInformation; public IO_COUNTERS IoInfo; public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct STARTUPINFO { public int cb; public string lpReserved, lpDesktop, lpTitle; public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags; public ushort wShowWindow, cbReserved2; public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError; }
        [StructLayout(LayoutKind.Sequential)] struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public uint dwProcessId, dwThreadId; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, uint size);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll", SetLastError = true)] static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool TerminateProcess(IntPtr process, uint exitCode);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool CreateProcess(string application, StringBuilder command, IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles, uint flags, IntPtr environment, string directory, ref STARTUPINFO startup, out PROCESS_INFORMATION process);
    }
}
