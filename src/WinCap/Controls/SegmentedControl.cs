using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace WinCap.Controls;

/// <summary>
/// Single-choice segmented control. <see cref="Tiles"/> = false draws a compact pill
/// (glyph and text side by side); true draws separate tiles (glyph above text).
/// </summary>
[ToolboxItem(true)]
[DefaultEvent(nameof(SelectedIndexChanged))]
public class SegmentedControl : Control
{
    private string[] _items = [];
    private string[] _glyphs = [];
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private bool _tiles;

    public event EventHandler? SelectedIndexChanged;

    public SegmentedControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        ForeColor = Theme.Text;
        AccessibleRole = AccessibleRole.PageTabList;
        Size = new Size(200, 28);
    }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string[] Items
    {
        get => _items;
        set
        {
            _items = value ?? [];
            if (_selectedIndex >= _items.Length) _selectedIndex = _items.Length - 1;
            Invalidate();
        }
    }

    /// <summary>Optional Segoe MDL2 Assets glyph per item.</summary>
    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string[] Glyphs
    {
        get => _glyphs;
        set { _glyphs = value ?? []; Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Tiles
    {
        get => _tiles;
        set { _tiles = value; Invalidate(); }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            value = Math.Clamp(value, -1, _items.Length - 1);
            if (value == _selectedIndex) return;
            _selectedIndex = value;
            AccessibleName = value >= 0 ? _items[value] : null;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int Gap => _tiles ? LogicalToDeviceUnits(6) : 0;

    private Rectangle SegmentBounds(int i)
    {
        int n = Math.Max(1, _items.Length);
        int gap = Gap;
        int pad = _tiles ? 0 : LogicalToDeviceUnits(3);
        int inner = ClientSize.Width - pad * 2 - gap * (n - 1);
        int x0 = pad + i * inner / n + i * gap;
        int x1 = pad + (i + 1) * inner / n + i * gap;
        return new Rectangle(x0, pad, x1 - x0, ClientSize.Height - pad * 2);
    }

    private int HitTest(Point p)
    {
        for (int i = 0; i < _items.Length; i++)
            if (SegmentBounds(i).Contains(p)) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int hit = HitTest(e.Location);
        if (hit != _hoverIndex) { _hoverIndex = hit; Invalidate(); }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverIndex = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Focus();
            int hit = HitTest(e.Location);
            if (hit >= 0) SelectedIndex = hit;
        }
        base.OnMouseDown(e);
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Left && _selectedIndex > 0) SelectedIndex--;
        else if (e.KeyCode == Keys.Right && _selectedIndex < _items.Length - 1) SelectedIndex++;
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnFontChanged(EventArgs e) { Invalidate(); base.OnFontChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float radius = LogicalToDeviceUnits(_tiles ? 8 : 7);
        if (!_tiles)
        {
            using var track = Theme.RoundRect(new RectangleF(0, 0, ClientSize.Width - 1, ClientSize.Height - 1), radius);
            using var b = new SolidBrush(Theme.Raised);
            g.FillPath(b, track);
        }

        using var glyphFont = new Font(Theme.IconFont, Font.Size * (_tiles ? 1.55f : 1.0f), GraphicsUnit.Point);
        for (int i = 0; i < _items.Length; i++)
        {
            Rectangle r = SegmentBounds(i);
            bool selected = i == _selectedIndex;
            bool hover = i == _hoverIndex && Enabled;
            string glyph = i < _glyphs.Length ? _glyphs[i] : string.Empty;

            if (_tiles)
                PaintTile(g, r, radius, _items[i], glyph, glyphFont, selected, hover);
            else
                PaintPill(g, r, radius - LogicalToDeviceUnits(2), _items[i], glyph, glyphFont, selected, hover);
        }

        if (Focused && ShowFocusCues && _selectedIndex >= 0)
        {
            Rectangle r = SegmentBounds(_selectedIndex);
            r.Inflate(-1, -1);
            using var path = Theme.RoundRect(r, radius);
            using var pen = new Pen(Color.FromArgb(160, Theme.Accent)) { DashStyle = DashStyle.Dot };
            g.DrawPath(pen, path);
        }
    }

    private void PaintTile(Graphics g, Rectangle r, float radius, string text, string glyph, Font glyphFont, bool selected, bool hover)
    {
        var rf = new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1.5f, r.Height - 1.5f);
        using (var path = Theme.RoundRect(rf, radius))
        {
            using var fill = new SolidBrush(selected ? Theme.RaisedSelected : hover ? Theme.RaisedHover : Theme.Raised);
            g.FillPath(fill, path);
            if (selected)
            {
                using var pen = new Pen(Theme.Accent, Math.Max(1.5f, LogicalToDeviceUnits(2) * 0.75f));
                g.DrawPath(pen, path);
            }
        }

        Color glyphColor = !Enabled ? Theme.TextDim : selected ? Theme.Accent : Theme.TextMuted;
        Color textColor = !Enabled ? Theme.TextDim : selected ? Theme.Text : Theme.TextMuted;
        int half = r.Height / 2;
        var glyphRect = new Rectangle(r.X, r.Y, r.Width, half + LogicalToDeviceUnits(4));
        var textRect = new Rectangle(r.X, r.Y + half + LogicalToDeviceUnits(4), r.Width, r.Height - half - LogicalToDeviceUnits(10));
        TextRenderer.DrawText(g, glyph, glyphFont, glyphRect, glyphColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, text, Font, textRect, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
    }

    private void PaintPill(Graphics g, Rectangle r, float radius, string text, string glyph, Font glyphFont, bool selected, bool hover)
    {
        if (selected || hover)
        {
            using var path = Theme.RoundRect(r, Math.Max(1, radius));
            using var fill = new SolidBrush(selected ? Theme.RaisedSelected : Theme.RaisedHover);
            g.FillPath(fill, path);
        }

        Color glyphColor = !Enabled ? Theme.TextDim : selected ? Theme.Accent : Theme.TextMuted;
        Color textColor = !Enabled ? Theme.TextDim : selected ? Theme.Text : Theme.TextMuted;
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;

        Size ts = TextRenderer.MeasureText(g, text, Font, Size.Empty, flags);
        if (glyph.Length == 0)
        {
            TextRenderer.DrawText(g, text, Font, r, textColor, flags | TextFormatFlags.HorizontalCenter);
            return;
        }
        Size gs = TextRenderer.MeasureText(g, glyph, glyphFont, Size.Empty, flags);
        int gap = LogicalToDeviceUnits(6);
        int x = r.X + (r.Width - (gs.Width + gap + ts.Width)) / 2;
        TextRenderer.DrawText(g, glyph, glyphFont, new Rectangle(x, r.Y, gs.Width, r.Height), glyphColor, flags);
        TextRenderer.DrawText(g, text, Font, new Rectangle(x + gs.Width + gap, r.Y, ts.Width, r.Height), textColor, flags);
    }
}
