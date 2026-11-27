using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace WinCap.Controls;

/// <summary>On/off switch rendered as a pill with a sliding knob.</summary>
[ToolboxItem(true)]
public class ToggleSwitch : CheckBox
{
    private Color _onColor = Theme.Accent;
    private Color _offColor = Color.FromArgb(62, 68, 78);

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AutoSize = false;
        Size = new Size(56, 28);
        Cursor = Cursors.Hand;
        Text = string.Empty;
    }

    [Category("Appearance")]
    [DefaultValue(typeof(Color), "197, 240, 90")]
    public Color OnColor
    {
        get => _onColor;
        set { _onColor = value; Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(typeof(Color), "62, 68, 78")]
    public Color OffColor
    {
        get => _offColor;
        set { _offColor = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float w = ClientSize.Width - 1f;
        float h = ClientSize.Height - 1f;

        using (var path = new GraphicsPath())
        {
            path.AddArc(0, 0, h, h, 90, 180);
            path.AddArc(w - h, 0, h, h, 270, 180);
            path.CloseFigure();
            Color track = Enabled ? (Checked ? _onColor : _offColor) : Theme.Raised;
            using var brush = new SolidBrush(track);
            g.FillPath(brush, path);
        }

        float inset = Math.Max(3f, h * 0.11f);
        float knob = h - inset * 2;
        float x = Checked ? w - inset - knob : inset;
        using (var knobBrush = new SolidBrush(!Enabled ? Theme.TextDim : Checked ? Theme.OnAccent : Theme.TextMuted))
            g.FillEllipse(knobBrush, x, inset, knob, knob);

        if (Focused && ShowFocusCues)
        {
            using var pen = new Pen(Color.FromArgb(160, Theme.Accent)) { DashStyle = DashStyle.Dot };
            g.DrawArc(pen, x - 1, inset - 1, knob + 2, knob + 2, 0, 360);
        }
    }
}
