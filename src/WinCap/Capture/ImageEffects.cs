using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WinCap.Capture;

internal static class ImageEffects
{
    /// <summary>
    /// Returns a copy of <paramref name="source"/> resized from <paramref name="dpi"/> to 96 DPI
    /// (100 %), e.g. 175 % (168 DPI): 1750 px -> 1000 px. Returns null when no resize is needed.
    /// </summary>
    public static Bitmap? ScaleToStandardDpi(Bitmap source, int dpi)
    {
        if (dpi <= 0 || dpi == 96) return null;
        int w = Math.Max(1, (int)Math.Round(source.Width * 96.0 / dpi));
        int h = Math.Max(1, (int)Math.Round(source.Height * 96.0 / dpi));
        if (w == source.Width && h == source.Height) return null;

        var result = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        result.SetResolution(96, 96);
        using var g = Graphics.FromImage(result);
        g.CompositingMode = CompositingMode.SourceCopy;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;
        using var attributes = new ImageAttributes();
        attributes.SetWrapMode(WrapMode.TileFlipXY); // no dark/transparent fringe at the edges
        g.DrawImage(source, new Rectangle(0, 0, w, h), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        return result;
    }

    /// <summary>Returns a new image with a solid border of the given colour and thickness around <paramref name="source"/>.</summary>
    public static Bitmap AddBorder(Bitmap source, Color color, int thickness)
    {
        thickness = Math.Max(1, thickness);
        var result = new Bitmap(source.Width + thickness * 2, source.Height + thickness * 2, PixelFormat.Format24bppRgb);
        result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
        using var g = Graphics.FromImage(result);
        g.Clear(color);
        g.CompositingMode = CompositingMode.SourceCopy;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(source, new Rectangle(thickness, thickness, source.Width, source.Height),
            new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
        return result;
    }
}
