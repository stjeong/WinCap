using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace WinCap.Controls;

/// <summary>Primary action button: rounded accent tile with a glyph above the caption.</summary>
[ToolboxItem(true)]
public class AccentButton : Button
{
    private string _glyph = string.Empty;
    private int _cornerRadius = 14;
    private bool _hover;
    private bool _pressed;

    public AccentButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        ForeColor = Theme.OnAccent;
    }

    /// <summary>Segoe MDL2 Assets glyph drawn above the text.</summary>
    [Category("Appearance")]
    [DefaultValue("")]
    public string Glyph
    {
        get => _glyph;
        set { _glyph = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Corner radius in logical (96 DPI) pixels.</summary>
    [Category("Appearance")]
    [DefaultValue(14)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color fill = !Enabled ? Theme.RaisedHover
            : _pressed ? Theme.AccentPressed
            : _hover ? Theme.AccentHover
            : Theme.Accent;
        Color fore = Enabled ? ForeColor : Theme.TextDim;

        var rect = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1.5f, ClientSize.Height - 1.5f);
        float radius = LogicalToDeviceUnits(_cornerRadius);
        using (var path = Theme.RoundRect(rect, radius))
        using (var brush = new SolidBrush(fill))
            g.FillPath(brush, path);

        int h = ClientSize.Height;
        if (_glyph.Length > 0)
        {
            using var glyphFont = new Font(Theme.IconFont, Font.Size * 2.1f, GraphicsUnit.Point);
            var glyphRect = new Rectangle(0, 0, ClientSize.Width, (int)(h * 0.58f));
            TextRenderer.DrawText(g, _glyph, glyphFont, glyphRect, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.NoPadding);
            var textRect = new Rectangle(0, (int)(h * 0.62f), ClientSize.Width, (int)(h * 0.3f));
            TextRenderer.DrawText(g, Text, Font, textRect, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
        }
        else
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }

        if (Focused && ShowFocusCues)
        {
            var inner = RectangleF.Inflate(rect, -LogicalToDeviceUnits(3), -LogicalToDeviceUnits(3));
            using var path = Theme.RoundRect(inner, Math.Max(1, radius - LogicalToDeviceUnits(3)));
            using var pen = new Pen(Color.FromArgb(140, Theme.OnAccent)) { DashStyle = DashStyle.Dot };
            g.DrawPath(pen, path);
        }
    }
}
