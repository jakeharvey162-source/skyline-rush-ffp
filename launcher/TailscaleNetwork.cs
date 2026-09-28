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

    // Discovery and reachability checks only. Does not log in, modify the VPN,
    // administer a tailnet, or expose host process controls.
    public static class TailscaleNetwork
    {
        static bool IsTailscaleIPv4(string value)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(value, out ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
            byte[] b = ip.GetAddressBytes();
            return b[0] == 100 && b[1] >= 64 && b[1] <= 127;
        }

        static string FindExecutable()
        {
            var candidates = new List<string>();
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!String.IsNullOrWhiteSpace(pf)) candidates.Add(Path.Combine(pf, "Tailscale", "tailscale.exe"));
            if (!String.IsNullOrWhiteSpace(pfx86)) candidates.Add(Path.Combine(pfx86, "Tailscale", "tailscale.exe"));

            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string folder in path.Split(Path.PathSeparator))
            {
                string clean = folder.Trim().Trim('"');
                if (!String.IsNullOrWhiteSpace(clean)) candidates.Add(Path.Combine(clean, "tailscale.exe"));
            }

            foreach (string candidate in candidates)
            {
                try { if (File.Exists(candidate)) return candidate; }
                catch (ArgumentException) { }
                catch (NotSupportedException) { }
                catch (PathTooLongException) { }
            }
            return null;
        }

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
                        string value = item as string;
                        if (IsTailscaleIPv4(value))
                            return new NetworkStatus { Connected = true, Address = value, Message = "Tailscale connected. Other players must have access to this private network." };
                    }
                }
                return new NetworkStatus { Message = "Tailscale has no usable IPv4 address yet. Reconnect and choose Check Again." };
            }
            catch (ArgumentException) { return new NetworkStatus { Message = "Could not read Tailscale status. Update or restart Tailscale, then choose Check Again." }; }
            catch (InvalidOperationException) { return new NetworkStatus { Message = "Tailscale returned an unreadable status. Restart Tailscale and try again." }; }
        }

        public static async Task<NetworkStatus> CheckAsync()
        {
            string exe = FindExecutable();
            if (String.IsNullOrWhiteSpace(exe))
                return new NetworkStatus { Message = "Skyline Rush Alpha online multiplayer currently uses Tailscale. Install Tailscale and join your host's private network, then choose Check Again. Offline Mode remains available." };

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo(exe, "status --json")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    process.Start();
                    Task<string> output = process.StandardOutput.ReadToEndAsync();
                    Task<string> errors = process.StandardError.ReadToEndAsync();
                    if (!await Task.Run(() => process.WaitForExit(5000)))
                    {
                        process.Kill();
                        await Task.Run(() => process.WaitForExit());
                        await Task.WhenAll(output, errors);
                        return new NetworkStatus { Message = "Tailscale did not respond in time. Restart Tailscale and choose Check Again." };
                    }
                    await Task.WhenAll(output, errors);
                    if (process.ExitCode != 0)
                        return new NetworkStatus { Message = "Tailscale is unavailable. Open Tailscale and reconnect, then choose Check Again." };
                    return ParseStatus(output.Result);
                }
            }
            catch (System.ComponentModel.Win32Exception) { return new NetworkStatus { Message = "Windows could not start the Tailscale status check. Open Tailscale and try again." }; }
            catch (IOException) { return new NetworkStatus { Message = "Could not read Tailscale status. Restart Tailscale and try again." }; }
        }

        public static async Task<NetworkStatus> CheckPeerAsync(string address)
        {
            if (!IsTailscaleIPv4(address))
                return new NetworkStatus { Message = "The room host address is not a valid Skyline Alpha Tailscale address." };

            string exe = FindExecutable();
            if (String.IsNullOrWhiteSpace(exe))
                return new NetworkStatus { Message = "Tailscale is not installed. Install it and join the host's private network before joining this room." };

            try
            {
                using (var process = new Process())
                {
                    // --until-direct=false accepts a valid relayed Tailscale path too; Alpha
                    // multiplayer should not fail just because NAT prevents a direct route.
                    string args = "ping --c 1 --timeout 3s --until-direct=false " + address;
                    process.StartInfo = new ProcessStartInfo(exe, args)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    process.Start();
                    Task<string> output = process.StandardOutput.ReadToEndAsync();
                    Task<string> errors = process.StandardError.ReadToEndAsync();
                    if (!await Task.Run(() => process.WaitForExit(5000)))
                    {
                        process.Kill();
                        await Task.Run(() => process.WaitForExit());
                        await Task.WhenAll(output, errors);
                        return new NetworkStatus { Message = "The host did not respond over Tailscale. Confirm you are on the host's shared private network and try again." };
                    }
                    await Task.WhenAll(output, errors);
                    if (process.ExitCode != 0 || output.Result.IndexOf("pong from", StringComparison.OrdinalIgnoreCase) < 0)
                        return new NetworkStatus { Message = "The room exists, but this PC cannot reach the host over Tailscale. Ask the host to confirm you have access to the same private network." };

                    return new NetworkStatus {
                        Connected = true,
                        Address = address,
                        Message = "Host reachable over Tailscale."
                    };
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return new NetworkStatus { Message = "Windows could not run the Tailscale host check. Restart Tailscale and try again." };
            }
            catch (IOException)
            {
                return new NetworkStatus { Message = "Could not verify the host over Tailscale. Check your connection and try again." };
            }
        }
    }
}
