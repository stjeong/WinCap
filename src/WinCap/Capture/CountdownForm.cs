using WinCap.Controls;
using WinCap.Native;

namespace WinCap.Capture;

/// <summary>
/// Small click-through, non-activating countdown window shown at the top-left of the desktop
/// while waiting for a delayed capture.
/// </summary>
internal sealed class CountdownForm : Form
{
    private int _value;
    private readonly Font _numberFont;
    private readonly Font _captionFont;

    private CountdownForm()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Theme.Header;
        Opacity = 0.88;

        float scale = DeviceDpi / 96f;
        Size = new Size((int)(130 * scale), (int)(150 * scale));
        Rectangle desktop = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 800, 600);
        Location = new Point(desktop.Left + (int)(20 * scale), desktop.Top + (int)(20 * scale));

        _numberFont = new Font("Segoe UI", 60f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        _captionFont = new Font("Segoe UI", 13f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE |
                          NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOPMOST;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.TrySetRoundedCorners(Handle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        var caption = new Rectangle(0, (int)(ClientSize.Height * 0.06), ClientSize.Width, (int)(ClientSize.Height * 0.25));
        TextRenderer.DrawText(g, "Capture in", _captionFont, caption, Color.FromArgb(200, 200, 200),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        var hint = new Rectangle(0, (int)(ClientSize.Height * 0.78), ClientSize.Width, (int)(ClientSize.Height * 0.18));
        var number = new Rectangle(0, caption.Bottom - (int)(ClientSize.Height * 0.04), ClientSize.Width, hint.Top - caption.Bottom + (int)(ClientSize.Height * 0.04));
        TextRenderer.DrawText(g, _value.ToString(), _numberFont, number, Theme.Accent,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, "Esc: cancel", _captionFont, hint, Color.FromArgb(150, 150, 150),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private const int VK_ESCAPE = 0x1B;
    private const int PollIntervalMs = 50;

    /// <summary>
    /// Shows the countdown for <paramref name="seconds"/> seconds, then hides it.
    /// Returns false when the user pressed Esc (in any application) to cancel the capture.
    /// </summary>
    /// <remarks>
    /// The countdown window never gets keyboard focus (no-activate, click-through), so Esc is
    /// detected by polling the global key state. This does not swallow Esc in other programs.
    /// </remarks>
    public static async Task<bool> RunAsync(int seconds)
    {
        using var form = new CountdownForm();
        form.Show();

        // Ignore an Esc that is already held down when the countdown starts; require a new press.
        bool escWasDown = IsEscDown();
        bool canceled = false;

        for (int i = seconds; i > 0 && !canceled; i--)
        {
            form._value = i;
            form.Refresh();

            var tick = System.Diagnostics.Stopwatch.StartNew();
            while (tick.ElapsedMilliseconds < 1000)
            {
                await Task.Delay(PollIntervalMs);
                bool escDown = IsEscDown();
                if (escDown && !escWasDown)
                {
                    canceled = true;
                    break;
                }
                escWasDown = escDown;
            }
        }

        form.Hide();
        if (!canceled)
        {
            await Task.Delay(150); // give DWM time to remove the window before capturing
        }
        return !canceled;
    }

    private static bool IsEscDown() => (NativeMethods.GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _numberFont.Dispose();
            _captionFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
