using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

internal static class SkylineLauncher
{
    [STAThread]
    private static int Main()
    {
        try
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkylineRush", "player");
            Directory.CreateDirectory(home);
            string exe = Path.Combine(root, "bin", "amd64", "redeclipse_windows_amd64.exe");
            if (!File.Exists(exe)) throw new FileNotFoundException("The game executable is missing. Reinstall the full game package.");
            ProcessStartInfo start = new ProcessStartInfo(exe);
            start.WorkingDirectory = root;
            start.Arguments = "-h\"" + home + "\"";
            start.UseShellExecute = false;
            using (Process game = Process.Start(start)) { game.WaitForExit(); return game.ExitCode; }
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Skyline Rush could not start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
