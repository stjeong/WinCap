using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static WinCap.Native.NativeMethods;

namespace WinCap.Capture;

/// <summary>Screen capture helpers. All rectangles are in physical (virtual-screen) pixels.</summary>
internal static class ScreenCapture
{
    /// <summary>Bounding rectangle of all monitors.</summary>
    public static Rectangle VirtualScreen => SystemInformation.VirtualScreen;

    /// <summary>Captures the given screen rectangle.</summary>
    public static Bitmap Capture(Rectangle bounds, bool includeCursor)
    {
        var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            if (includeCursor)
            {
                DrawCursor(g, bounds.Location);
            }
        }
        return bmp;
    }

    /// <summary>
    /// Effective DPI of the monitor that contains most of <paramref name="area"/>
    /// (96 = 100 %, 168 = 175 %). Falls back to 96 when it cannot be determined.
    /// </summary>
    public static int GetDpiForArea(Rectangle area)
    {
        try
        {
            var r = new RECT { Left = area.Left, Top = area.Top, Right = area.Right, Bottom = area.Bottom };
            IntPtr monitor = MonitorFromRect(ref r, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 && dpiX > 0)
                return (int)dpiX;
        }
        catch
        {
            // shcore.dll unavailable: assume 100 %.
        }
        return 96;
    }

    /// <summary>Copies a sub-rectangle of <paramref name="source"/> into a new bitmap.</summary>
    public static Bitmap Crop(Bitmap source, Rectangle rect)
    {
        var result = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(result);
        g.CompositingMode = CompositingMode.SourceCopy;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(source, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
        return result;
    }

    /// <summary>Draws the current mouse cursor; <paramref name="origin"/> is the screen position of the bitmap's (0,0).</summary>
    private static void DrawCursor(Graphics g, Point origin)
    {
        var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
        if (!GetCursorInfo(ref ci) || (ci.flags & CURSOR_SHOWING) == 0 || ci.hCursor == IntPtr.Zero)
        {
            return;
        }

        int hotX = 0, hotY = 0;
        if (GetIconInfo(ci.hCursor, out var ii))
        {
            hotX = ii.xHotspot;
            hotY = ii.yHotspot;
            if (ii.hbmMask != IntPtr.Zero) DeleteObject(ii.hbmMask);
            if (ii.hbmColor != IntPtr.Zero) DeleteObject(ii.hbmColor);
        }

        IntPtr hdc = g.GetHdc();
        try
        {
            DrawIconEx(hdc,
                ci.ptScreenPos.X - hotX - origin.X,
                ci.ptScreenPos.Y - hotY - origin.Y,
                ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
        }
        finally
        {
            g.ReleaseHdc(hdc);
        }
    }
}
