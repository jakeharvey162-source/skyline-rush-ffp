using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SkylineRush;
class HostTests
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
    static void Ready(WindowsMatchHost.Match match)
    {
        for (int i = 0; i < 100 && !match.IsReady && !match.HasExited; i++) Thread.Sleep(100);
        Check(match.IsReady, "actual dedicated server startup " + match.Port);
    }
    static int Main(string[] args)
    {
        if (args.Length == 3 && args[1] == "worker")
        {
            using (var worker = new WindowsMatchHost(args[0], args[2], 41301, 1))
            {
                var match = worker.Start(Guid.NewGuid(), "duel", "heights"); Ready(match);
                File.WriteAllText(Path.Combine(args[2], "pid"), match.ProcessId.ToString());
                Thread.Sleep(Timeout.Infinite);
            }
            return 0;
        }
        Check(!TailscaleNetwork.ParseStatus("{}").Connected, "missing VPN status rejected");
        Check(!TailscaleNetwork.ParseStatus("not json").Connected, "malformed VPN status rejected");
        Check(!TailscaleNetwork.ParseStatus("{\"BackendState\":\"Stopped\",\"TailscaleIPs\":[\"100.64.1.2\"]}").Connected, "stale disconnected IP rejected");
        Check(!TailscaleNetwork.ParseStatus("{\"BackendState\":\"Running\",\"TailscaleIPs\":[\"192.168.1.2\"]}").Connected, "LAN address not advertised as VPN");
        Check(TailscaleNetwork.ParseStatus("{\"BackendState\":\"Running\",\"TailscaleIPs\":[\"fd7a::1\",\"100.64.1.2\"]}").Address == "100.64.1.2", "VPN IPv4 selected");
        string state = Path.Combine(Path.GetTempPath(), "SkylineHostTest-" + Guid.NewGuid());
        try
        {
            using (var host = new WindowsMatchHost(args[0], state, 41201, 2))
            {
                bool locked = false;
                try { using (var duplicate = new WindowsMatchHost(args[0], state, 41201, 2)) {} } catch (IOException) { locked = true; }
                Check(locked, "duplicate manager rejected");
                bool invalid = false;
                try { host.Start(Guid.NewGuid(), "duel;quit", "heights"); } catch (ArgumentException) { invalid = true; }
                Check(invalid, "invalid mode rejected");
                var a = host.Start(Guid.NewGuid(), "duel", "heights"); Ready(a);
                var b = host.Start(Guid.NewGuid(), "2v2", "lagoon"); Ready(b);
                Check(a.Port != b.Port && Math.Abs(a.Port - b.Port) == 2, "separate game/query pairs");
                Check(a.Password != b.Password && a.Password.Length == 24, "unique random passwords");
                bool full = false;
                try { host.Start(Guid.NewGuid(), "coop4", "rift"); } catch (InvalidOperationException) { full = true; }
                Check(full, "capacity enforced");
                string home = a.Home; host.Stop(a.Id);
                Check(!File.Exists(Path.Combine(home, "servinit.cfg")), "credentials removed on stop");
                Process.GetProcessById(b.ProcessId).Kill(); Thread.Sleep(300);
                Check(host.Reap(TimeSpan.FromHours(2)).Contains(b.Id), "crashed server reaped");
                using (var blocked = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    blocked.Bind(new IPEndPoint(IPAddress.Any, 41202));
                    var c = host.Start(Guid.NewGuid(), "coop2", "rift"); Ready(c);
                    Check(c.Port == 41203, "query port conflict avoided");
                    Check(host.Reap(TimeSpan.Zero).Contains(c.Id), "maximum lifetime cleanup");
                }
            }
            string workerState = state + "-worker";
            var start = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,
                "\"" + args[0] + "\" worker \"" + workerState + "\"");
            start.UseShellExecute = false;
            using (var owner = Process.Start(start))
            {
                try
                {
                    string pidFile = Path.Combine(workerState, "pid");
                    for (int i = 0; i < 150 && !File.Exists(pidFile) && !owner.HasExited; i++) Thread.Sleep(100);
                    Check(File.Exists(pidFile), "host crash fixture started");
                    using (var server = Process.GetProcessById(int.Parse(File.ReadAllText(pidFile))))
                    {
                        owner.Kill(); owner.WaitForExit();
                        Check(server.WaitForExit(5000), "host crash kills owned dedicated server");
                    }
                    using (var recovered = new WindowsMatchHost(args[0], workerState, 41301, 1))
                        Check(Directory.GetFiles(workerState, "servinit.cfg", SearchOption.AllDirectories).Length == 0, "stale credentials cleaned on restart");
                }
                finally { if (!owner.HasExited) { owner.Kill(); owner.WaitForExit(); } }
            }
            Directory.Delete(workerState, true);
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        finally { if (Directory.Exists(state)) Directory.Delete(state, true); }
    }
}
