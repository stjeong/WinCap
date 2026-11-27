namespace WinCap;

/// <summary>
/// The application icon (blue rounded square, white selection brackets, red shutter dot).
/// Loaded from the embedded multi-size app.ico, which is also the .exe icon (ApplicationIcon).
/// </summary>
internal static class AppIcon
{
    private const string ResourceName = "WinCap.app.ico";

    public static Icon Create()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
        return new Icon(stream);
    }

    /// <summary>Returns the best-matching icon frame as a bitmap of the given pixel size.</summary>
    public static Bitmap CreateBitmap(Icon icon, int size)
    {
        using var sized = new Icon(icon, size, size);
        var frame = sized.ToBitmap();
        if (frame.Width == size && frame.Height == size) return frame;

        // No exact frame (e.g. odd DPI scale): resample the nearest one.
        using (frame)
        {
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(frame, 0, 0, size, size);
            return bmp;
        }
    }
}
