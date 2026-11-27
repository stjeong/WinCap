using System.ComponentModel;

namespace WinCap.Hotkeys;

/// <summary>Read-only text box that records a key combination when the user presses it.</summary>
[ToolboxItem(false)]
internal sealed class HotkeyBox : TextBox
{
    public HotkeyBox()
    {
        ReadOnly = true;
        BackColor = SystemColors.Window;
        ShortcutsEnabled = false;
        TextAlign = HorizontalAlignment.Center;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Keys HotkeyCode { get; private set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Keys HotkeyModifiers { get; private set; }

    /// <summary>Raised whenever the recorded key combination changes.</summary>
    public event EventHandler? HotkeyChanged;

    public void SetHotkey(Keys modifiers, Keys key)
    {
        HotkeyModifiers = modifiers & Keys.Modifiers;
        HotkeyCode = key;
        HotkeyChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The text is a read-only display: never show it as selected.</summary>
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        BeginInvoke(() => Select(Text.Length, 0));
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (Focused) Select(Text.Length, 0);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        Keys mods = keyData & Keys.Modifiers;

        // Let plain Tab / Esc / Enter keep their dialog meaning.
        if (mods == Keys.None && key is Keys.Tab or Keys.Escape or Keys.Enter)
            return base.ProcessCmdKey(ref msg, keyData);

        if (mods == Keys.None && key is Keys.Back or Keys.Delete)
        {
            SetHotkey(Keys.None, Keys.None);
            return true;
        }

        SetHotkey(mods, HotkeyHelper.IsModifierKey(key) ? Keys.None : key);
        return true;
    }
}
