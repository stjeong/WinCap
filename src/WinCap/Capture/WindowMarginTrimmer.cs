using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static WinCap.Native.NativeMethods;

namespace WinCap.Capture;

/// <summary>
/// Removes the transparent margin that some layered windows (WPF/Electron with custom chrome,
/// e.g. Snagit) keep around their visible content for a drop shadow.
/// </summary>
/// <remarks>
/// The window is rendered on its own with PrintWindow(PW_RENDERFULLCONTENT). Fully transparent
/// pixels (and the shadow) come out as pure black (0,0,0), so whole black rows/columns at the
/// edges are the margin. A side is only trimmed when content is found within
/// <see cref="MaxMargin"/> pixels; this protects windows that failed to render (all black)
/// or are genuinely dark at the edge.
/// </remarks>
internal static class WindowMarginTrimmer
{
    /// <summary>Largest margin (in physical pixels) that is considered transparent padding.</summary>
    public const int MaxMargin = 40;

    /// <summary>Returns <paramref name="bounds"/> without the transparent margin (unchanged on failure).</summary>
    public static Rectangle Trim(IntPtr hwnd, Rectangle bounds)
    {
        if (bounds.Width <= MaxMargin * 2 || bounds.Height <= MaxMargin * 2 || IsHungAppWindow(hwnd))
            return bounds;

        try
        {
            using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                IntPtr hdc = g.GetHdc();
                try
                {
                    if (!PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT))
                        return bounds;
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }

            int[] px = ReadPixels(bmp);
            int w = bmp.Width, h = bmp.Height;

            bool IsBlack(int x, int y) => (px[y * w + x] & 0x00FFFFFF) == 0;
            bool ColumnBlack(int x) { for (int y = 0; y < h; y++) if (!IsBlack(x, y)) return false; return true; }
            bool RowBlack(int y) { for (int x = 0; x < w; x++) if (!IsBlack(x, y)) return false; return true; }

            int left = Measure(i => ColumnBlack(i));
            int right = Measure(i => ColumnBlack(w - 1 - i));
            int top = Measure(i => RowBlack(i));
            int bottom = Measure(i => RowBlack(h - 1 - i));

            if (left + right + top + bottom == 0)
                return bounds;

            return Rectangle.FromLTRB(bounds.Left + left, bounds.Top + top, bounds.Right - right, bounds.Bottom - bottom);
        }
        catch
        {
            return bounds;
        }
    }

    /// <summary>Number of leading black lines, or 0 when no content is found within <see cref="MaxMargin"/>.</summary>
    private static int Measure(Func<int, bool> isBlackLine)
    {
        for (int i = 0; i < MaxMargin; i++)
        {
            if (!isBlackLine(i)) return i;
        }
        return 0;
    }

    private static int[] ReadPixels(Bitmap bmp)
    {
        var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var px = new int[bmp.Width * bmp.Height];
            for (int y = 0; y < bmp.Height; y++)
                Marshal.Copy(data.Scan0 + y * data.Stride, px, y * bmp.Width, bmp.Width);
            return px;
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }
}
