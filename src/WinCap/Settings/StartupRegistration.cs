using Microsoft.Win32;

namespace WinCap.Settings;

/// <summary>
/// Run-at-logon registration via HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// The registry value itself is the source of truth (not settings.json), so the
/// option stays correct even if the user removes the entry elsewhere (e.g. Task Manager).
/// </summary>
public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WinCap";

    /// <summary>Full path of the running executable.</summary>
    private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

    private static string Command => $"\"{ExePath}\"";

    /// <summary>True if a Run entry exists that points at this executable.</summary>
    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                if (key?.GetValue(ValueName) is not string value) return false;
                return string.Equals(value.Trim().Trim('"'), ExePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Adds or removes the Run entry. Throws on registry errors.</summary>
    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Cannot open the Run registry key.");
        if (enabled)
            key.SetValue(ValueName, Command, RegistryValueKind.String);
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
