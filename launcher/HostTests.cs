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
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        finally { if (Directory.Exists(state)) Directory.Delete(state, true); }
    }
}
