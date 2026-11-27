using WinCap.Settings;

namespace WinCap.Dialogs;

/// <summary>Time-delay option: number of seconds to wait before capturing.</summary>
public sealed class DelayDialog : DialogBase
{
    private readonly NumericUpDown _numSeconds;

    public int Seconds => (int)_numSeconds.Value;

    public DelayDialog(int seconds)
    {
        Text = "Time Delay";
        Table.ColumnCount = 3;

        _numSeconds = new NumericUpDown
        {
            Minimum = AppSettings.MinDelay,
            Maximum = AppSettings.MaxDelay,
            Value = Math.Clamp(seconds, AppSettings.MinDelay, AppSettings.MaxDelay),
            Width = 70,
            Anchor = AnchorStyles.Left,
        };
        Table.Controls.Add(MakeLabel("Delay before capture:"), 0, 0);
        Table.Controls.Add(_numSeconds, 1, 0);
        Table.Controls.Add(MakeLabel("seconds"), 2, 0);

        var note = new Label
        {
            Text = "A countdown is shown at the top-left of the desktop while waiting.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 6, 0, 0),
        };
        Table.Controls.Add(note, 0, 1);
        Table.SetColumnSpan(note, 3);

        Shown += (_, _) => { _numSeconds.Focus(); _numSeconds.Select(0, _numSeconds.Text.Length); };
        FinishLayout();
    }
}
