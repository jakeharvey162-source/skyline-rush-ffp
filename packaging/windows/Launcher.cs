using System;
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
            string exe = Path.Combine(root, "bin", "amd64", "redeclipse_windows_amd64.exe");
            if (!File.Exists(exe)) throw new FileNotFoundException("The game executable is missing. Reinstall the full Skyline Rush package.");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SkylineRush.SkylineLauncherForm(root));
            return 0;
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Skyline Rush could not start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
