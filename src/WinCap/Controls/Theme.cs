using System.Drawing.Drawing2D;

namespace WinCap.Controls;

/// <summary>Colours and drawing helpers for the dark "graphite + lime" look.</summary>
internal static class Theme
{
    public static readonly Color Header = Color.FromArgb(18, 20, 24);
    public static readonly Color Surface = Color.FromArgb(26, 29, 34);
    public static readonly Color Raised = Color.FromArgb(36, 40, 47);
    public static readonly Color RaisedHover = Color.FromArgb(48, 53, 61);
    public static readonly Color RaisedSelected = Color.FromArgb(62, 68, 78);
    public static readonly Color Border = Color.FromArgb(52, 57, 66);
    public static readonly Color Divider = Color.FromArgb(38, 42, 49);

    public static readonly Color Text = Color.FromArgb(230, 232, 235);
    public static readonly Color TextMuted = Color.FromArgb(150, 157, 167);
    public static readonly Color TextDim = Color.FromArgb(104, 111, 121);

    public static readonly Color Accent = Color.FromArgb(197, 240, 90);
    public static readonly Color AccentHover = Color.FromArgb(214, 248, 132);
    public static readonly Color AccentPressed = Color.FromArgb(168, 210, 68);
    public static readonly Color OnAccent = Color.FromArgb(22, 25, 18);

    public static readonly Color Warning = Color.FromArgb(255, 176, 92);
    public static readonly Color CloseHover = Color.FromArgb(196, 43, 28);

    public const string IconFont = "Segoe MDL2 Assets";

    public static GraphicsPath RoundRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0)
        {
            path.AddRectangle(r);
            return path;
        }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
