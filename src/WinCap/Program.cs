using WinCap.Updates;

namespace WinCap;

internal static class Program
{
    /// <summary>The main entry point for the application.</summary>
    [STAThread]
    private static void Main(string[] args)
    {
        // Restarted by a portable update: the previous process is still shutting down.
        bool afterUpdate = args.Contains(UpdateInstaller.AfterUpdateArgument, StringComparer.OrdinalIgnoreCase);

        // Single instance: a global hotkey can only be registered once.
        using var mutex = new Mutex(false, @"Local\WinCap.SingleInstance");
        bool owned;
        try
        {
            owned = mutex.WaitOne(afterUpdate ? TimeSpan.FromSeconds(30) : TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            owned = true; // the previous instance ended without releasing it
        }
        if (!owned)
        {
            MessageBox.Show("WinCap is already running.", "WinCap", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            UpdateInstaller.DeleteOldExe();

            // Generated from the .csproj: high DPI mode (PerMonitorV2), visual styles, default font.
            ApplicationConfiguration.Initialize();
#pragma warning disable WFO5001 // Dark mode is experimental in .NET 9: gives the dialogs dark controls and title bars.
            Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001
            Application.Run(new MainForm(afterUpdate));
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
