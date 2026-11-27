using static WinCap.Native.NativeMethods;

namespace WinCap.Hotkeys;

internal static class HotkeyHelper
{
    /// <summary>Converts WinForms modifier keys (+ optional Win key) to RegisterHotKey MOD_* flags.</summary>
    public static uint ToNativeModifiers(Keys modifiers, bool win)
    {
        uint mods = 0;
        if ((modifiers & Keys.Control) != 0) mods |= MOD_CONTROL;
        if ((modifiers & Keys.Shift) != 0) mods |= MOD_SHIFT;
        if ((modifiers & Keys.Alt) != 0) mods |= MOD_ALT;
        if (win) mods |= MOD_WIN;
        return mods;
    }

    public static bool IsModifierKey(Keys key) => key is
        Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or
        Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or
        Keys.Menu or Keys.LMenu or Keys.RMenu or
        Keys.LWin or Keys.RWin;

    public static bool IsFunctionKey(Keys key) => key >= Keys.F1 && key <= Keys.F24;

    /// <summary>A hotkey needs at least one modifier, unless it is a function key.</summary>
    public static bool IsValid(uint mods, Keys key) =>
        key != Keys.None && !IsModifierKey(key) && (mods != 0 || IsFunctionKey(key));

    /// <summary>
    /// True when no other program has registered the combination (probe: register + unregister).
    /// The caller must have released its own registration of the same hotkey first.
    /// </summary>
    public static bool IsAvailable(IntPtr hwnd, uint mods, Keys key)
    {
        const int probeId = 0xBFFE;
        if (!RegisterHotKey(hwnd, probeId, mods | MOD_NOREPEAT, (uint)key)) return false;
        UnregisterHotKey(hwnd, probeId);
        return true;
    }

    /// <summary>Human-readable text such as "Ctrl+Shift+Alt+R".</summary>
    public static string Format(uint mods, Keys key)
    {
        var parts = new List<string>(5);
        if ((mods & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((mods & MOD_SHIFT) != 0) parts.Add("Shift");
        if ((mods & MOD_ALT) != 0) parts.Add("Alt");
        if ((mods & MOD_WIN) != 0) parts.Add("Win");
        if (key != Keys.None) parts.Add(KeyName(key));
        return string.Join("+", parts);
    }

    private static string KeyName(Keys key) => key switch
    {
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => "Num" + (key - Keys.NumPad0),
        Keys.Snapshot => "PrintScreen",
        Keys.Prior => "PageUp",
        Keys.Next => "PageDown",
        Keys.Return => "Enter",
        Keys.Oemcomma => ",",
        Keys.OemPeriod => ".",
        Keys.OemMinus => "-",
        Keys.Oemplus => "=",
        _ => key.ToString(),
    };
}
