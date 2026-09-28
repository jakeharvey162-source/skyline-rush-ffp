using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SkylineRush
{
    public sealed class NetworkStatus
    {
        public bool Connected;
        public string Address;
        public string Message;
    }
    // Discovery only. Does not log in, modify the VPN, or expose host process controls.
    public static class TailscaleNetwork
    {
        public static NetworkStatus ParseStatus(string json)
        {
            try
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = 1048576 };
                var status = serializer.Deserialize<Dictionary<string, object>>(json);
                object backend, addresses;
                if (status == null || !status.TryGetValue("BackendState", out backend) || !String.Equals(backend as string, "Running", StringComparison.Ordinal))
                    return new NetworkStatus { Message = "Tailscale is disconnected. Open Tailscale, connect to your shared private network, then choose Check Again." };
                if (status.TryGetValue("TailscaleIPs", out addresses) && addresses is IEnumerable)
                {
                    foreach (object item in (IEnumerable)addresses)
                    {
                        IPAddress ip;
                        if (!IPAddress.TryParse(item as string, out ip) || ip.AddressFamily != AddressFamily.InterNetwork) continue;
                        byte[] b = ip.GetAddressBytes();
                        if (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                            return new NetworkStatus { Connected = true, Address = ip.ToString(), Message = "Tailscale connected. Other players must have access to this private network." };
                    }
                }
                return new NetworkStatus { Message = "Tailscale has no usable IPv4 address yet. Reconnect and choose Check Again." };
            }
            catch (ArgumentException) { return new NetworkStatus { Message = "Could not read Tailscale status. Update or restart Tailscale, then choose Check Again." }; }
            catch (InvalidOperationException) { return new NetworkStatus { Message = "Tailscale returned an unreadable status. Restart Tailscale and try again." }; }
        }
        public static async Task<NetworkStatus> CheckAsync()
        {
            string exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tailscale", "tailscale.exe");
            if (!File.Exists(exe)) return new NetworkStatus { Message = "Skyline Rush Alpha online multiplayer currently uses Tailscale. Install Tailscale and join your host's private network, then choose Check Again. Offline Mode remains available." };
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo(exe, "status --json") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                    process.Start();
                    Task<string> output = process.StandardOutput.ReadToEndAsync();
                    Task<string> errors = process.StandardError.ReadToEndAsync();
                    if (!await Task.Run(() => process.WaitForExit(5000)))
                    {
                        process.Kill(); await Task.Run(() => process.WaitForExit());
                        await Task.WhenAll(output, errors);
                        return new NetworkStatus { Message = "Tailscale did not respond in time. Restart Tailscale and choose Check Again." };
                    }
                    await Task.WhenAll(output, errors);
                    if (process.ExitCode != 0) return new NetworkStatus { Message = "Tailscale is unavailable. Open Tailscale and reconnect, then choose Check Again." };
                    return ParseStatus(output.Result);
                }
            }
            catch (System.ComponentModel.Win32Exception) { return new NetworkStatus { Message = "Windows could not start the Tailscale status check. Open Tailscale and try again." }; }
            catch (IOException) { return new NetworkStatus { Message = "Could not read Tailscale status. Restart Tailscale and try again." }; }
        }
    }
}
