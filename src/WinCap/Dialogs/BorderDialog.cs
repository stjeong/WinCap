using WinCap.Settings;

namespace WinCap.Dialogs;

/// <summary>Border effect options: colour and thickness.</summary>
public sealed class BorderDialog : DialogBase
{
    private readonly Button _btnColor;
    private readonly NumericUpDown _numThickness;
    private readonly Panel _preview;

    public Color BorderColor { get; private set; }
    public int BorderThickness => (int)_numThickness.Value;

    public BorderDialog(Color color, int thickness)
    {
        Text = "Border";
        BorderColor = color;
        Table.ColumnCount = 3;

        _btnColor = new Button { Width = 120, Height = 30, TextAlign = ContentAlignment.MiddleRight };
        _btnColor.Click += (_, _) => PickColor();
        Table.Controls.Add(MakeLabel("Color:"), 0, 0);
        Table.Controls.Add(_btnColor, 1, 0);

        _numThickness = new NumericUpDown
        {
            Minimum = AppSettings.MinBorder,
            Maximum = AppSettings.MaxBorder,
            Value = Math.Clamp(thickness, AppSettings.MinBorder, AppSettings.MaxBorder),
            Width = 70,
            Anchor = AnchorStyles.Left,
        };
        _numThickness.ValueChanged += (_, _) => _preview?.Invalidate();
        Table.Controls.Add(MakeLabel("Thickness:"), 0, 1);
        Table.Controls.Add(_numThickness, 1, 1);
        Table.Controls.Add(MakeLabel("px"), 2, 1);

        _preview = new Panel { Size = new Size(220, 110), Margin = new Padding(0, 10, 0, 0), BackColor = Color.White };
        _preview.Paint += PaintPreview;
        Table.Controls.Add(_preview, 0, 2);
        Table.SetColumnSpan(_preview, 3);

        UpdateColorButton();
        FinishLayout();
    }

    private void PickColor()
    {
        using var dlg = new ColorDialog { Color = BorderColor, FullOpen = true, AnyColor = true };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            BorderColor = dlg.Color;
            UpdateColorButton();
            _preview.Invalidate();
        }
    }

    private void UpdateColorButton()
    {
        _btnColor.Text = $"#{BorderColor.R:X2}{BorderColor.G:X2}{BorderColor.B:X2}";
        _btnColor.Image?.Dispose();
        int s = LogicalToDeviceUnits(16);
        var swatch = new Bitmap(s, s);
        using (var g = Graphics.FromImage(swatch))
        {
            g.Clear(BorderColor);
            g.DrawRectangle(Pens.Gray, 0, 0, s - 1, s - 1);
        }
        _btnColor.Image = swatch;
        _btnColor.ImageAlign = ContentAlignment.MiddleLeft;
    }

    private void PaintPreview(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        int t = LogicalToDeviceUnits(BorderThickness);
        var inner = Rectangle.Inflate(_preview.ClientRectangle, -LogicalToDeviceUnits(20), -LogicalToDeviceUnits(16));
        var outer = Rectangle.Inflate(inner, t, t);
        using (var b = new SolidBrush(BorderColor)) g.FillRectangle(b, outer);
        using (var b = new SolidBrush(Color.FromArgb(225, 235, 245))) g.FillRectangle(b, inner);
        TextRenderer.DrawText(g, "Preview", Font, inner, Color.FromArgb(90, 100, 110),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _btnColor.Image?.Dispose();
        base.Dispose(disposing);
    }
}
