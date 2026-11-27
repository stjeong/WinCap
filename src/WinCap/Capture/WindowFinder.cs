using static WinCap.Native.NativeMethods;

namespace WinCap.Capture;

/// <param name="MayHaveTransparentMargin">
/// Layered window without a native (DWM) frame, e.g. WPF/Electron windows that draw their own
/// drop shadow inside a transparent margin. See <see cref="WindowMarginTrimmer"/>.
/// </param>
internal sealed record WindowInfo(IntPtr Handle, string Title, Rectangle Bounds, bool MayHaveTransparentMargin);

/// <summary>Enumerates visible top-level windows in Z-order (topmost first).</summary>
internal static class WindowFinder
{
    public static List<WindowInfo> GetVisibleWindows()
    {
        var result = new List<WindowInfo>();
        uint ownPid = (uint)Environment.ProcessId;

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || IsIconic(hwnd)) return true;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == ownPid) return true;

            if (DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
                return true; // e.g. UWP windows on other virtual desktops

            long exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            if ((exStyle & WS_EX_TRANSPARENT) != 0) return true; // click-through overlays

            string title = GetTitle(hwnd);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0 && title.Length == 0) return true;

            Rectangle bounds = GetBounds(hwnd);
            if (bounds.Width <= 1 || bounds.Height <= 1) return true;

            // Without a native frame the DWM bounds equal the window rect, so a transparent
            // shadow margin of a layered window is not excluded by GetBounds().
            bool layered = (exStyle & WS_EX_LAYERED) != 0;
            bool noNativeFrame = GetWindowRect(hwnd, out RECT wr) && wr.ToRectangle() == bounds;

            if (!noNativeFrame)
            {
                bounds = ExcludeVisibleBorder(hwnd, bounds);
            }

            result.Add(new WindowInfo(hwnd, title, bounds, layered && noNativeFrame));
            return true;
        }, IntPtr.Zero);

        return result;
    }

    /// <summary>Window bounds without the invisible resize border (DWM extended frame bounds).</summary>
    private static Rectangle GetBounds(IntPtr hwnd)
    {
        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT r, System.Runtime.InteropServices.Marshal.SizeOf<RECT>()) == 0)
            return r.ToRectangle();
        return GetWindowRect(hwnd, out r) ? r.ToRectangle() : Rectangle.Empty;
    }

    /// <summary>
    /// Windows 11 draws a thin border (1 px, DPI scaled) around framed windows. It is part of the
    /// DWM extended frame bounds, so remove it on all four sides. On Chromium-based windows (Edge,
    /// Chrome, VS Code ...) it covers the top edge of the content as well.
    /// Maximized windows have no border; Windows 10 does not support the attribute (no change).
    /// </summary>
    private static Rectangle ExcludeVisibleBorder(IntPtr hwnd, Rectangle bounds)
    {
        if (IsZoomed(hwnd)) return bounds;
        if (DwmGetWindowAttribute(hwnd, DWMWA_VISIBLE_FRAME_BORDER_THICKNESS, out int border, sizeof(int)) != 0)
            return bounds;
        if (border <= 0 || border * 2 >= bounds.Width || border * 2 >= bounds.Height)
            return bounds;
        return Rectangle.Inflate(bounds, -border, -border);
    }

    private static string GetTitle(IntPtr hwnd)
    {
        int len = GetWindowTextLength(hwnd);
        if (len <= 0) return string.Empty;
        var buffer = new char[len + 1];
        int copied = GetWindowText(hwnd, buffer, buffer.Length);
        return new string(buffer, 0, copied);
    }
}
