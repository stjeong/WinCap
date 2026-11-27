using System.Diagnostics;
using System.Drawing.Imaging;
using WinCap.Capture;
using WinCap.Controls;
using WinCap.Dialogs;
using WinCap.Hotkeys;
using WinCap.Native;
using WinCap.Settings;

namespace WinCap;

public partial class MainForm : Form
{
    private const int HotkeyId = 0x0001;

    private readonly AppSettings _settings;
    private bool _busy;
    private bool _loadingUi;
    private bool _hotkeyRegistered;
    private string? _lastFilePath;

    public MainForm()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        mnuMain.Renderer = new DarkMenuRenderer();
        segMode.SelectedIndex = 0; // Image
        Icon = AppIcon.Create();
        UpdateLogoImage();
        lblLogo.SizeChanged += (_, _) => UpdateLogoImage(); // re-render after DPI scaling

        toolTip.SetToolTip(btnSettings, "Settings (save folder, image format, editor)");
        toolTip.SetToolTip(btnMore, "Menu");
        toolTip.SetToolTip(btnEffectsSettings, "Border color and thickness");
        toolTip.SetToolTip(btnDelayOptions, "Set delay time");
        toolTip.SetToolTip(chkTrimMargins,
            "Window mode: exclude the invisible shadow margin that some windows\n" +
            "(e.g. WPF/Electron apps with custom title bars) have around their content.");
        toolTip.SetToolTip(btnReset, "Reset capture options to defaults");
        toolTip.SetToolTip(btnMinimize, "Minimize");
        toolTip.SetToolTip(btnClose, "Close");
        toolTip.SetToolTip(lblStatus, "Double-click to open the capture folder");

        ApplySettingsToUi();
    }

    // ------------------------------------------------------------------
    //  Window chrome (borderless form with custom title bar)
    // ------------------------------------------------------------------

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style |= NativeMethods.WS_MINIMIZEBOX | NativeMethods.WS_SYSMENU; // taskbar minimize/restore
            cp.ClassStyle |= NativeMethods.CS_DROPSHADOW;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.TrySetRoundedCorners(Handle);
        RegisterHotkey(showError: false);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        UnregisterHotkey();
        base.OnHandleDestroyed(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _settings.Save();
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            _ = RunCaptureAsync(repeatLast: true);
            return;
        }
        base.WndProc(ref m);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, IntPtr.Zero);
    }

    private void btnMore_Click(object? sender, EventArgs e) =>
        mnuMain.Show(btnMore, new Point(btnMore.Width, btnMore.Height), ToolStripDropDownDirection.BelowLeft);

    private void btnMinimize_Click(object? sender, EventArgs e) => WindowState = FormWindowState.Minimized;

    private void btnClose_Click(object? sender, EventArgs e) => Close();

    // ------------------------------------------------------------------
    //  Settings <-> UI
    // ------------------------------------------------------------------

    private void ApplySettingsToUi()
    {
        _loadingUi = true;
        try
        {
            segSelection.SelectedIndex = (int)_settings.Mode;
            chkTrimMargins.Checked = _settings.TrimWindowMargins;
            chkTrimMargins.Enabled = _settings.Mode == CaptureMode.Window;
            segEffects.SelectedIndex = _settings.BorderEnabled ? 1 : 0;
            tglPreview.Checked = _settings.PreviewInEditor;
            tglClipboard.Checked = _settings.CopyToClipboard;
            tglCursor.Checked = _settings.CaptureCursor;
            tglDelay.Checked = _settings.TimeDelayEnabled;
            mnuViewAlwaysOnTop.Checked = _settings.AlwaysOnTop;
            TopMost = _settings.AlwaysOnTop;
            UpdateDelayLabel();
            UpdateHotkeyLabel();
            UpdateOpenFolderTooltip();
        }
        finally
        {
            _loadingUi = false;
        }
    }

    private void UpdateLogoImage()
    {
        int size = Math.Min(lblLogo.Width, lblLogo.Height);
        if (size <= 0 || Icon == null) return;
        var old = lblLogo.Image;
        lblLogo.Image = AppIcon.CreateBitmap(Icon, size);
        old?.Dispose();
    }

    private void UpdateOpenFolderTooltip() =>
        toolTip.SetToolTip(btnOpenFolder, $"Open capture folder in Explorer\n{_settings.SaveFolder}");

    private void UpdateDelayLabel() => lblDelay.Text = $"Time delay · {_settings.DelaySeconds}s";

    private void UpdateHotkeyLabel()
    {
        bool has = _settings.HotkeyKey != Keys.None;
        string text = has ? HotkeyHelper.Format(_settings.HotkeyModifiers, _settings.HotkeyKey) : string.Empty;
        // Registration happens once the window handle exists; before that the state is unknown.
        bool inUse = has && IsHandleCreated && !_hotkeyRegistered;

        lnkHotkey.Text = !has ? "Add Hotkey" : inUse ? $"{text} (in use)" : text;
        lnkHotkey.LinkColor = inUse ? Theme.Warning : Theme.TextMuted;
        toolTip.SetToolTip(lnkHotkey,
            !has ? "Assign a global hotkey that re-captures the last captured area."
            : inUse ? $"{text} is used by another program, so WinCap cannot receive it. Click to choose another combination."
            : $"Global hotkey {text}: re-captures the last captured area. Click to change.");
    }

    private void SetStatus(string text) => lblStatus.Text = text;

    private void segSelection_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loadingUi || segSelection.SelectedIndex < 0) return;
        _settings.Mode = (CaptureMode)segSelection.SelectedIndex;
        chkTrimMargins.Enabled = _settings.Mode == CaptureMode.Window;
        _settings.Save();
    }

    private void chkTrimMargins_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loadingUi) return;
        _settings.TrimWindowMargins = chkTrimMargins.Checked;
        _settings.Save();
    }

    private void segEffects_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loadingUi) return;
        _settings.BorderEnabled = segEffects.SelectedIndex == 1;
        _settings.Save();
    }

    private void tglPreview_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loadingUi) return;
        _settings.PreviewInEditor = tglPreview.Checked;
        _settings.Save();
    }

    private void tglClipboard_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loadingUi) return;
        _settings.CopyToClipboard = tglClipboard.Checked;
        _settings.Save();
    }

    private void tglCursor_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loadingUi) return;
        _settings.CaptureCursor = tglCursor.Checked;
        _settings.Save();
    }

    private void tglDelay_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loadingUi) return;
        _settings.TimeDelayEnabled = tglDelay.Checked;
        _settings.Save();
    }

    private void btnDelayOptions_Click(object? sender, EventArgs e)
    {
        using var dlg = new DelayDialog(_settings.DelaySeconds);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _settings.DelaySeconds = dlg.Seconds;
        _settings.TimeDelayEnabled = true;
        _settings.Save();
        ApplySettingsToUi();
    }

    private void btnEffectsSettings_Click(object? sender, EventArgs e)
    {
        using var dlg = new BorderDialog(_settings.BorderColorValue, _settings.BorderThickness);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _settings.BorderColorValue = dlg.BorderColor;
        _settings.BorderThickness = dlg.BorderThickness;
        _settings.BorderEnabled = true;
        _settings.Save();
        ApplySettingsToUi();
    }

    private void btnReset_Click(object? sender, EventArgs e)
    {
        _settings.ResetCaptureOptions();
        _settings.Save();
        ApplySettingsToUi();
        SetStatus("Capture options were reset to defaults.");
    }

    private void segMode_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (segMode.SelectedIndex != 1) return;
        segMode.SelectedIndex = 0; // only Image is implemented
        MessageBox.Show(this, "Video capture is not available yet.\nOnly image capture is implemented in this version.",
            "WinCap", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ------------------------------------------------------------------
    //  Hotkey
    // ------------------------------------------------------------------

    private bool RegisterHotkey(bool showError)
    {
        UnregisterHotkey();
        if (_settings.HotkeyKey == Keys.None || !IsHandleCreated) return true;

        _hotkeyRegistered = NativeMethods.RegisterHotKey(Handle, HotkeyId,
            _settings.HotkeyModifiers | NativeMethods.MOD_NOREPEAT, (uint)_settings.HotkeyKey);

        if (!_hotkeyRegistered)
        {
            string text = HotkeyHelper.Format(_settings.HotkeyModifiers, _settings.HotkeyKey);
            SetStatus($"Hotkey {text} could not be registered (in use by another program).");
            if (showError)
            {
                MessageBox.Show(this, $"The hotkey {text} is already in use by another program.\nPlease choose a different combination.",
                    "Hotkey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        UpdateHotkeyLabel();
        return _hotkeyRegistered;
    }

    private void UnregisterHotkey()
    {
        if (_hotkeyRegistered)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            _hotkeyRegistered = false;
        }
    }

    private void lnkHotkey_LinkClicked(object? sender, EventArgs e)
    {
        // Release the current hotkey so that the same combination can be typed into the dialog.
        UnregisterHotkey();
        try
        {
            using var dlg = new HotkeyDialog(_settings.HotkeyModifiers, _settings.HotkeyKey);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _settings.HotkeyModifiers = dlg.Modifiers;
                _settings.HotkeyKey = dlg.Key;
                _settings.Save();
                UpdateHotkeyLabel();
                if (RegisterHotkey(showError: true))
                {
                    SetStatus(dlg.Key == Keys.None
                        ? "Hotkey removed."
                        : $"Hotkey set: {HotkeyHelper.Format(dlg.Modifiers, dlg.Key)}");
                }
                return;
            }
        }
        finally
        {
            if (!_hotkeyRegistered) RegisterHotkey(showError: false);
        }
    }

    // ------------------------------------------------------------------
    //  Capture
    // ------------------------------------------------------------------

    private async void btnCapture_Click(object? sender, EventArgs e) => await RunCaptureAsync(repeatLast: false);

    /// <param name="repeatLast">true when triggered by the hotkey: capture the last captured area again.</param>
    private async Task RunCaptureAsync(bool repeatLast)
    {
        if (_busy) return;
        _busy = true;

        bool wasVisible = Visible && WindowState != FormWindowState.Minimized;
        Bitmap? image = null;
        try
        {
            if (wasVisible)
            {
                Hide();
                await Task.Delay(250); // let DWM finish hiding the window
            }

            if (_settings.TimeDelayEnabled && _settings.DelaySeconds > 0)
            {
                if (!await CountdownForm.RunAsync(_settings.DelaySeconds))
                {
                    SetStatus("Capture canceled (Esc during time delay).");
                    return; // finally{} restores the main window
                }
            }

            (image, Rectangle area) = AcquireImage(repeatLast);
            if (image == null)
            {
                SetStatus("Capture canceled.");
                return;
            }

            _settings.LastCapture = CaptureRect.From(area);

            // File: optionally shrink to 100 % scale (monitor DPI), then apply the border so its
            // thickness is exact in the saved file. Clipboard: original screen pixels.
            int dpi = ScreenCapture.GetDpiForArea(area);
            using Bitmap? scaled = _settings.ScaleToDpi ? ImageEffects.ScaleToStandardDpi(image, dpi) : null;
            using Bitmap fileImage = WithBorder(scaled ?? image);

            string path = SaveImage(fileImage);
            _lastFilePath = path;

            var notes = new List<string>();
            if (scaled != null)
            {
                notes.Add($"scaled {dpi * 100 / 96}% → 100% from {image.Width}×{image.Height}");
            }
            if (_settings.CopyToClipboard)
            {
                using Bitmap clipImage = WithBorder(image);
                notes.Add(TryCopyToClipboard(clipImage) ? "copied to clipboard" : "clipboard copy failed");
            }
            if (_settings.PreviewInEditor)
            {
                OpenInEditor(path);
            }

            SetStatus($"Saved {fileImage.Width}×{fileImage.Height}: {path}" + (notes.Count > 0 ? $" ({string.Join(", ", notes)})" : string.Empty));
            _settings.Save();
        }
        catch (Exception ex)
        {
            SetStatus("Capture failed.");
            MessageBox.Show(this, $"Capture failed:\n{ex.Message}", "WinCap", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            image?.Dispose();
            if (wasVisible)
            {
                Show();
                if (!_settings.PreviewInEditor) Activate();
            }
            _busy = false;
        }
    }

    /// <summary>Returns a new bitmap with the Border effect applied (or an unchanged copy).</summary>
    private Bitmap WithBorder(Bitmap source) =>
        _settings.BorderEnabled
            ? ImageEffects.AddBorder(source, _settings.BorderColorValue, _settings.BorderThickness)
            : new Bitmap(source);

    /// <summary>Captures according to the current Selection mode. Returns (null, _) when canceled.</summary>
    private (Bitmap? Image, Rectangle Area) AcquireImage(bool repeatLast)
    {
        Rectangle virtualScreen = ScreenCapture.VirtualScreen;
        bool cursor = _settings.CaptureCursor;

        if (repeatLast && _settings.LastCapture is { } last)
        {
            Rectangle r = Rectangle.Intersect(last.ToRectangle(), virtualScreen);
            if (r.Width > 0 && r.Height > 0)
                return (ScreenCapture.Capture(r, cursor), r);
        }

        switch (_settings.Mode)
        {
            case CaptureMode.Fullscreen:
                return (ScreenCapture.Capture(virtualScreen, cursor), virtualScreen);

            case CaptureMode.Window:
            case CaptureMode.Region:
            default:
            {
                var kind = _settings.Mode == CaptureMode.Window ? SelectionKind.Window : SelectionKind.Region;
                // Enumerate windows before the overlay appears so it is not part of the list.
                var windows = kind == SelectionKind.Window ? WindowFinder.GetVisibleWindows() : [];
                using var snapshot = ScreenCapture.Capture(virtualScreen, cursor);
                Func<WindowInfo, Rectangle>? resolveBounds = _settings.TrimWindowMargins
                    ? w => w.MayHaveTransparentMargin ? WindowMarginTrimmer.Trim(w.Handle, w.Bounds) : w.Bounds
                    : null;
                using var overlay = new SelectionOverlayForm(snapshot, virtualScreen, kind, windows, resolveBounds);
                if (overlay.ShowDialog() != DialogResult.OK)
                    return (null, Rectangle.Empty);

                Rectangle selected = overlay.SelectedBounds;
                Rectangle relative = selected;
                relative.Offset(-virtualScreen.X, -virtualScreen.Y);
                relative.Intersect(new Rectangle(Point.Empty, snapshot.Size));
                if (relative.Width <= 0 || relative.Height <= 0)
                    return (null, Rectangle.Empty);

                return (ScreenCapture.Crop(snapshot, relative), selected);
            }
        }
    }

    private string SaveImage(Bitmap image)
    {
        string folder = string.IsNullOrWhiteSpace(_settings.SaveFolder) ? AppSettings.DefaultSaveFolder : _settings.SaveFolder;
        Directory.CreateDirectory(folder);

        (string ext, ImageFormat format) = _settings.ImageFormat.ToUpperInvariant() switch
        {
            "JPG" or "JPEG" => (".jpg", ImageFormat.Jpeg),
            "BMP" => (".bmp", ImageFormat.Bmp),
            _ => (".png", ImageFormat.Png),
        };

        string path = Path.Combine(folder, $"Capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}{ext}");

        if (format.Guid == ImageFormat.Jpeg.Guid)
        {
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
            image.Save(path, codec, parameters);
        }
        else
        {
            image.Save(path, format);
        }
        return path;
    }

    private static bool TryCopyToClipboard(Image image)
    {
        try
        {
            // copy: true -> data stays on the clipboard after the image is disposed / app exits.
            Clipboard.SetDataObject(new DataObject(DataFormats.Bitmap, image), true, 5, 100);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OpenInEditor(string path)
    {
        string editor = string.IsNullOrWhiteSpace(_settings.EditorPath) ? AppSettings.DefaultEditor : _settings.EditorPath;
        try
        {
            Process.Start(new ProcessStartInfo(editor)
            {
                Arguments = $"\"{path}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not start the editor \"{editor}\":\n{ex.Message}\n\nCheck Settings (gear icon in the title bar).",
                "Preview in Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ------------------------------------------------------------------
    //  Menus
    // ------------------------------------------------------------------

    private void mnuFileSettings_Click(object? sender, EventArgs e)
    {
        using var dlg = new SettingsDialog(_settings);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _settings.SaveFolder = dlg.SaveFolder;
        _settings.ImageFormat = dlg.ImageFormat;
        _settings.ScaleToDpi = dlg.ScaleToDpi;
        _settings.EditorPath = dlg.EditorPath;
        _settings.Save();

        if (dlg.RunAtStartup != StartupRegistration.IsEnabled)
        {
            try
            {
                StartupRegistration.SetEnabled(dlg.RunAtStartup);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Cannot update the Windows startup entry:\n{ex.Message}", "Settings",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        UpdateOpenFolderTooltip();
        SetStatus($"Settings saved. Captures go to {_settings.SaveFolder}");
    }

    private void mnuFileOpenFolder_Click(object? sender, EventArgs e) => OpenCaptureFolder();

    private void lblStatus_DoubleClick(object? sender, EventArgs e) => OpenCaptureFolder();

    private void btnOpenFolder_Click(object? sender, EventArgs e) => OpenCaptureFolder();

    /// <summary>Draws an accent folder glyph followed by the caption, centered in the button.</summary>
    private void btnOpenFolder_Paint(object? sender, PaintEventArgs e)
    {
        const string glyph = ""; // OpenFolderHorizontal (Segoe MDL2 Assets)
        const string caption = "Open Folder";
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;

        // Glyph font follows the button font, so it tracks DPI scaling with it.
        using (var glyphFont = new Font(Theme.IconFont, btnOpenFolder.Font.Size * 1.1f))
        {
            Size gs = TextRenderer.MeasureText(e.Graphics, glyph, glyphFont, Size.Empty, flags);
            Size cs = TextRenderer.MeasureText(e.Graphics, caption, btnOpenFolder.Font, Size.Empty, flags);
            int gap = LogicalToDeviceUnits(5);
            int x = (btnOpenFolder.Width - (gs.Width + gap + cs.Width)) / 2;
            var r = btnOpenFolder.ClientRectangle;
            TextRenderer.DrawText(e.Graphics, glyph, glyphFont, new Rectangle(x, r.Y, gs.Width, r.Height), Theme.Accent, flags);
            TextRenderer.DrawText(e.Graphics, caption, btnOpenFolder.Font, new Rectangle(x + gs.Width + gap, r.Y, cs.Width, r.Height), btnOpenFolder.ForeColor, flags);
        }
    }

    private void OpenCaptureFolder()
    {
        try
        {
            Directory.CreateDirectory(_settings.SaveFolder);
            if (_lastFilePath != null && File.Exists(_lastFilePath))
                Process.Start("explorer.exe", $"/select,\"{_lastFilePath}\"");
            else
                Process.Start(new ProcessStartInfo(_settings.SaveFolder) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "WinCap", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void mnuFileExit_Click(object? sender, EventArgs e) => Close();

    private void mnuEditCopyLast_Click(object? sender, EventArgs e)
    {
        if (!TryGetLastFile(out string path)) return;
        using var bmp = LoadBitmap(path);
        SetStatus(TryCopyToClipboard(bmp) ? "Last capture copied to clipboard." : "Clipboard copy failed.");
    }

    private void mnuEditOpenLast_Click(object? sender, EventArgs e)
    {
        if (TryGetLastFile(out string path)) OpenInEditor(path);
    }

    private bool TryGetLastFile(out string path)
    {
        path = _lastFilePath ?? string.Empty;
        if (path.Length > 0 && File.Exists(path)) return true;
        MessageBox.Show(this, "There is no capture yet.", "WinCap", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    /// <summary>Loads an image without keeping the file locked.</summary>
    private static Bitmap LoadBitmap(string path)
    {
        using var stream = new MemoryStream(File.ReadAllBytes(path));
        using var img = Image.FromStream(stream);
        return new Bitmap(img);
    }

    private void mnuViewAlwaysOnTop_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loadingUi) return;
        _settings.AlwaysOnTop = mnuViewAlwaysOnTop.Checked;
        TopMost = _settings.AlwaysOnTop;
        _settings.Save();
    }

    private void mnuHelpAbout_Click(object? sender, EventArgs e)
    {
        string version = Application.ProductVersion;
        MessageBox.Show(this,
            $"WinCap {version}\n\nScreen capture tool (.NET 9 / Windows Forms).\n\n" +
            $"Settings: {AppSettings.SettingsFile}",
            "About WinCap", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
