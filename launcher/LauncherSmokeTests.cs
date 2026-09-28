using System;
using SkylineRush;

class LauncherSmokeTests
{
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    static int Main()
    {
        try
        {
            Check(SkylineSupabaseClient.NormalizeRoomCode(" ab12ef ") == "AB12EF", "room code normalized");
            bool badCode = false;
            try { SkylineSupabaseClient.NormalizeRoomCode("ZZZZZZ"); } catch (ArgumentException) { badCode = true; }
            Check(badCode, "invalid room code rejected");

            Check(SkylineSupabaseClient.IsTailscaleIPv4("100.64.1.2"), "Tailscale IPv4 accepted");
            Check(SkylineSupabaseClient.IsTailscaleIPv4("100.127.255.254"), "Tailscale upper-range IPv4 accepted");
            Check(!SkylineSupabaseClient.IsTailscaleIPv4("100.128.0.1"), "non-Tailscale CGNAT address rejected");
            Check(!SkylineSupabaseClient.IsTailscaleIPv4("192.168.1.4"), "LAN address rejected");

            var good = new SkylineRoomInfo {
                RoomId = Guid.NewGuid(),
                HostAddress = "100.64.1.2",
                ServerPort = 29801,
                ServerPassword = "Abcd_123"
            };
            SkylineSupabaseClient.ValidateConnection(good);
            Check(true, "valid private room connection accepted");

            bool badPassword = false;
            try {
                good.ServerPassword = "bad password";
                SkylineSupabaseClient.ValidateConnection(good);
            } catch (ArgumentException) { badPassword = true; }
            Check(badPassword, "unsafe server password rejected");

            SkylineSessionStore.Clear();
            SkylineSessionStore.SaveRefreshToken("test-refresh-token");
            Check(SkylineSessionStore.LoadRefreshToken() == "test-refresh-token", "refresh token DPAPI round-trip");
            SkylineSessionStore.Clear();
            Check(SkylineSessionStore.LoadRefreshToken() == null, "refresh token cleanup");

            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }
        finally
        {
            SkylineSessionStore.Clear();
        }
    }
}
