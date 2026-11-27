namespace WinCap;

internal static class Program
{
    /// <summary>The main entry point for the application.</summary>
    [STAThread]
    private static void Main()
    {
        // Single instance: a global hotkey can only be registered once.
        using var mutex = new Mutex(true, @"Local\WinCap.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("WinCap is already running.", "WinCap", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Generated from the .csproj: high DPI mode (PerMonitorV2), visual styles, default font.
        ApplicationConfiguration.Initialize();
#pragma warning disable WFO5001 // Dark mode is experimental in .NET 9: gives the dialogs dark controls and title bars.
        Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001
        Application.Run(new MainForm());
    }
}
