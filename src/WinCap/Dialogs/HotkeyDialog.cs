using WinCap.Hotkeys;
using static WinCap.Native.NativeMethods;

namespace WinCap.Dialogs;

/// <summary>Lets the user record the global hotkey that re-captures the last captured area.</summary>
public sealed class HotkeyDialog : DialogBase
{
    /// <summary>Suggested hotkey when none is configured yet.</summary>
    public const uint SuggestedModifiers = MOD_CONTROL | MOD_SHIFT | MOD_ALT;
    public const Keys SuggestedKey = Keys.R;

    private readonly HotkeyBox _box;
    private readonly CheckBox _chkWin;
    private readonly Label _lblError;

    /// <summary>Selected MOD_* flags (0 with <see cref="Key"/> = None means "no hotkey").</summary>
    public uint Modifiers { get; private set; }
    public Keys Key { get; private set; }

    public HotkeyDialog(uint modifiers, Keys key)
    {
        Text = "Hotkey";
        Table.ColumnCount = 2;

        if (key == Keys.None)
        {
            modifiers = SuggestedModifiers;
            key = SuggestedKey;
        }

        var info = new Label
        {
            Text = "Press the key combination to use as a global hotkey.\n" +
                   "The hotkey captures the last captured area again\n" +
                   "(a normal capture is started if there is none yet).",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
        };
        Table.Controls.Add(info, 0, 0);
        Table.SetColumnSpan(info, 2);

        _box = new HotkeyBox { Width = 230, Font = new Font("Segoe UI", 11F) };
        _chkWin = new CheckBox { Text = "+ Win key", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(10, 6, 0, 0) };
        Table.Controls.Add(_box, 0, 1);
        Table.Controls.Add(_chkWin, 1, 1);

        _lblError = new Label { AutoSize = true, MaximumSize = new Size(380, 0), ForeColor = WinCap.Controls.Theme.Warning, Margin = new Padding(0, 6, 0, 0) };
        Table.Controls.Add(_lblError, 0, 2);
        Table.SetColumnSpan(_lblError, 2);

        var btnClear = MakeButton("Remove Hotkey");
        btnClear.Margin = new Padding(0);
        btnClear.Click += (_, _) =>
        {
            Modifiers = 0;
            Key = Keys.None;
            DialogResult = DialogResult.OK;
        };
        ExtraButtonPanel.Controls.Add(btnClear);

        Keys formsMods = Keys.None;
        if ((modifiers & MOD_CONTROL) != 0) formsMods |= Keys.Control;
        if ((modifiers & MOD_SHIFT) != 0) formsMods |= Keys.Shift;
        if ((modifiers & MOD_ALT) != 0) formsMods |= Keys.Alt;
        _chkWin.Checked = (modifiers & MOD_WIN) != 0;
        _box.HotkeyChanged += (_, _) => UpdateText();
        _chkWin.CheckedChanged += (_, _) => UpdateText();
        _box.SetHotkey(formsMods, key);

        OkButton.Click += OnOk;
        Shown += (_, _) => _box.Focus();
        FinishLayout();
    }

    private uint CurrentModifiers() => HotkeyHelper.ToNativeModifiers(_box.HotkeyModifiers, _chkWin.Checked);

    private void UpdateText()
    {
        uint mods = CurrentModifiers();
        string text = HotkeyHelper.Format(mods, _box.HotkeyCode);
        _box.Text = text.Length == 0 ? "(press keys)" : _box.HotkeyCode == Keys.None ? text + "+..." : text;
        _lblError.Text = string.Empty;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        uint mods = CurrentModifiers();
        if (!HotkeyHelper.IsValid(mods, _box.HotkeyCode))
        {
            _lblError.Text = "Use at least one of Ctrl / Shift / Alt / Win with a key (or a function key).";
            DialogResult = DialogResult.None;
            return;
        }
        if (!HotkeyHelper.IsAvailable(Handle, mods, _box.HotkeyCode))
        {
            _lblError.Text = $"{HotkeyHelper.Format(mods, _box.HotkeyCode)} is already used by another program " +
                             "(for example another capture tool). Choose a different combination.";
            DialogResult = DialogResult.None;
            return;
        }
        Modifiers = mods;
        Key = _box.HotkeyCode;
    }
}
