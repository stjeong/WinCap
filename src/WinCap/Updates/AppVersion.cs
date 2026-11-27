using System.Reflection;

namespace WinCap.Updates;

internal static class AppVersion
{
    /// <summary>Version of the running build (the csproj / release tag version, e.g. 1.0.1).</summary>
    public static Version Current { get; } = ReadCurrent();

    private static Version ReadCurrent()
    {
        // InformationalVersion looks like "1.0.1+<commit hash>".
        string? info = typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (info != null && TryParse(info, out Version v)) return v;
        Version? asm = typeof(AppVersion).Assembly.GetName().Version;
        return asm != null ? new Version(asm.Major, asm.Minor, Math.Max(0, asm.Build)) : new Version(0, 0, 0);
    }

    /// <summary>Parses "v1.2.3", "1.2.3" or "1.2.3+hash" into a three-part version.</summary>
    public static bool TryParse(string text, out Version version)
    {
        string s = text.Trim();
        if (s.StartsWith('v') || s.StartsWith('V')) s = s[1..];
        int cut = s.IndexOfAny(['+', '-']);
        if (cut >= 0) s = s[..cut];

        if (Version.TryParse(s, out Version? v))
        {
            version = new Version(v.Major, v.Minor, Math.Max(0, v.Build));
            return true;
        }
        version = new Version(0, 0, 0);
        return false;
    }
}
