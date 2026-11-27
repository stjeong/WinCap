using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinCap.Settings;

/// <summary>Area to capture.</summary>
public enum CaptureMode
{
    Region,
    Window,
    Fullscreen,
}

/// <summary>Screen rectangle (virtual-screen pixel coordinates) of the last capture.</summary>
public sealed record CaptureRect(int X, int Y, int Width, int Height)
{
    public Rectangle ToRectangle() => new(X, Y, Width, Height);

    public static CaptureRect From(Rectangle r) => new(r.X, r.Y, r.Width, r.Height);
}

/// <summary>User settings, persisted as JSON in %APPDATA%\WinCap\settings.json.</summary>
public sealed class AppSettings
{
    public const string DefaultEditor = "mspaint.exe";
    public const int MinDelay = 1;
    public const int MaxDelay = 60;
    public const int MinBorder = 1;
    public const int MaxBorder = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCap");

    public static string SettingsFile => Path.Combine(SettingsDirectory, "settings.json");

    public static string DefaultSaveFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WinCap");

    // ---- File / Settings dialog ----
    public string SaveFolder { get; set; } = DefaultSaveFolder;
    public string ImageFormat { get; set; } = "PNG";
    /// <summary>Save files at 100 % scale: divide the pixel size by the monitor's DPI scale (e.g. 175 % -> x 1/1.75).</summary>
    public bool ScaleToDpi { get; set; } = true;
    public string EditorPath { get; set; } = DefaultEditor;

    // ---- Capture options (main window) ----
    public CaptureMode Mode { get; set; } = CaptureMode.Region;
    /// <summary>Window mode: cut off the transparent (shadow) margin of layered windows.</summary>
    public bool TrimWindowMargins { get; set; } = true;
    public bool BorderEnabled { get; set; } = true;
    public string BorderColor { get; set; } = "#404040";
    public int BorderThickness { get; set; } = 2;
    public bool PreviewInEditor { get; set; } = true;
    public bool CopyToClipboard { get; set; } = true;
    public bool CaptureCursor { get; set; }
    public bool TimeDelayEnabled { get; set; }
    public int DelaySeconds { get; set; } = 3;

    // ---- Hotkey (RegisterHotKey MOD_* flags + virtual key) ----
    public uint HotkeyModifiers { get; set; }
    public Keys HotkeyKey { get; set; } = Keys.None;

    // ---- Misc ----
    public bool AlwaysOnTop { get; set; }
    public CaptureRect? LastCapture { get; set; }

    [JsonIgnore]
    public Color BorderColorValue
    {
        get
        {
            try { return ColorTranslator.FromHtml(BorderColor); }
            catch { return Color.FromArgb(64, 64, 64); }
        }
        set => BorderColor = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
    }

    /// <summary>Restores the capture options shown on the main window to their defaults.</summary>
    public void ResetCaptureOptions()
    {
        var d = new AppSettings();
        Mode = d.Mode;
        TrimWindowMargins = d.TrimWindowMargins;
        BorderEnabled = d.BorderEnabled;
        BorderColor = d.BorderColor;
        BorderThickness = d.BorderThickness;
        PreviewInEditor = d.PreviewInEditor;
        CopyToClipboard = d.CopyToClipboard;
        CaptureCursor = d.CaptureCursor;
        TimeDelayEnabled = d.TimeDelayEnabled;
        DelaySeconds = d.DelaySeconds;
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile), JsonOptions);
                if (settings != null)
                {
                    settings.Normalize();
                    return settings;
                }
            }
        }
        catch
        {
            // Corrupted settings file: fall back to defaults.
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Settings are not critical; ignore write failures.
        }
    }

    private void Normalize()
    {
        if (string.IsNullOrWhiteSpace(SaveFolder)) SaveFolder = DefaultSaveFolder;
        if (string.IsNullOrWhiteSpace(EditorPath)) EditorPath = DefaultEditor;
        if (string.IsNullOrWhiteSpace(ImageFormat)) ImageFormat = "PNG";
        if (!Enum.IsDefined(Mode)) Mode = CaptureMode.Region;
        DelaySeconds = Math.Clamp(DelaySeconds, MinDelay, MaxDelay);
        BorderThickness = Math.Clamp(BorderThickness, MinBorder, MaxBorder);
    }
}
