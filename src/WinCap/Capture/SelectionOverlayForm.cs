using WinCap.Controls;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using WinCap.Native;

namespace WinCap.Capture;

internal enum SelectionKind
{
    /// <summary>User drags a rectangle with the mouse.</summary>
    Region,

    /// <summary>User clicks the window under the mouse.</summary>
    Window,
}

/// <summary>
/// Full virtual-screen overlay showing a frozen screenshot. The user drags a region or
/// picks a window; the selection is returned in screen coordinates.
/// </summary>
internal sealed class SelectionOverlayForm : Form
{
    private readonly Bitmap _snapshot;
    private readonly Bitmap _dimmed;
    private readonly Rectangle _virtual;
    private readonly SelectionKind _kind;
    private readonly IReadOnlyList<WindowInfo> _windows;
    private readonly Func<WindowInfo, Rectangle>? _resolveBounds;
    private readonly Dictionary<IntPtr, Rectangle> _resolvedBounds = new();
    private readonly Font _labelFont;
    private readonly Font _hintFont;
    private readonly Pen _selectionPen;

    private bool _dragging;         // left button held down (drag selection)
    private bool _spaceSelecting;   // started with Space: the region follows the mouse without a button
    private bool _spaceKeyDown;     // suppresses keyboard auto-repeat while Space is held
    private bool _finishOnMouseUp;  // left click that finishes a Space selection
    private readonly SlowMotion _slow = new(); // Shift held: cursor moves at quarter speed
    private Point _dragStart;
    private Rectangle _selection;   // client coordinates
    private Rectangle _lastDirty;
    private Rectangle _hintRect;
    private string _hintText = string.Empty;

    // ---- Magnifier (Region mode) ----
    private const int MagSourceSize = 40;   // source pixels per side (cursor pixel = index Size/2)
    private const int MagZoom = 5;          // logical px per source pixel (scaled by DPI)
    private readonly bool _showMagnifier;
    private readonly int _magCell;          // device px per source pixel
    private Point _cursorClient;            // mouse position in client coordinates
    private Rectangle _magRect;             // magnifier incl. info panel, client coordinates
    private Rectangle _primaryClient;       // primary monitor in client coordinates
    private int _magWidth;                  // magnifier width: zoom size or widest info text

    /// <summary>Selected area in screen (virtual-screen) coordinates.</summary>
    public Rectangle SelectedBounds { get; private set; }

    /// <param name="resolveBounds">
    /// Optional: maps a window to its effective bounds (e.g. without a transparent margin).
    /// Called lazily for windows under the mouse; results are cached.
    /// </param>
    public SelectionOverlayForm(Bitmap snapshot, Rectangle virtualScreen, SelectionKind kind,
        IReadOnlyList<WindowInfo> windows, Func<WindowInfo, Rectangle>? resolveBounds = null)
    {
        _resolveBounds = resolveBounds;
        _snapshot = snapshot;
        _virtual = virtualScreen;
        _kind = kind;
        _windows = windows;
        _dimmed = CreateDimmed(snapshot);

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);

        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        BackColor = Color.Black;
        Cursor = Cursors.Cross;
        Bounds = virtualScreen;
        Text = "WinCap Selection";

        float scale = DeviceDpi / 96f;
        _labelFont = new Font("Segoe UI", 13f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        _hintFont = new Font("Segoe UI", 16f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        _showMagnifier = kind == SelectionKind.Region;
        _magCell = Math.Max(MagZoom, (int)Math.Round(MagZoom * scale));
        _selectionPen = new Pen(Theme.Accent, Math.Max(2f, 2f * scale)) { Alignment = PenAlignment.Inset };
    }

    private static Bitmap CreateDimmed(Bitmap source)
    {
        var bmp = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.DrawImageUnscaled(source, 0, 0);
        using var shade = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        g.FillRectangle(shade, 0, 0, bmp.Width, bmp.Height);
        return bmp;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Bounds = _virtual; // re-apply in case the system adjusted it
        Activate();

        _hintText = _kind == SelectionKind.Region
            ? "Drag to select a region, or press Space to start and Space / click to finish   ·   Shift: slow mouse   ·   Esc / right-click to cancel"
            : "Click a window to capture   ·   Esc / right-click to cancel";
        Size textSize = TextRenderer.MeasureText(_hintText, _hintFont);
        Rectangle primary = Screen.PrimaryScreen?.Bounds ?? _virtual;
        primary.Offset(-_virtual.X, -_virtual.Y);
        int padX = LogicalToDeviceUnits(16), padY = LogicalToDeviceUnits(8);
        _hintRect = new Rectangle(
            primary.X + (primary.Width - textSize.Width) / 2 - padX,
            primary.Y + LogicalToDeviceUnits(24),
            textSize.Width + padX * 2,
            textSize.Height + padY * 2);

        _primaryClient = primary;
        int widestText = Math.Max(
            TextRenderer.MeasureText("X -99999   Y -99999", _labelFont).Width,
            TextRenderer.MeasureText("RGB 255, 255, 255   #FFFFFF", _labelFont).Width);
        _magWidth = Math.Max(MagZoomSize, widestText + LogicalToDeviceUnits(16));
        if (_kind == SelectionKind.Window)
        {
            UpdateWindowHighlight(PointToClient(Cursor.Position));
        }
        else
        {
            UpdateMagnifier(PointToClient(Cursor.Position));
        }
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        // The overlay spans monitors with different DPI; never let WinForms rescale it.
        if (m.Msg == NativeMethods.WM_DPICHANGED)
        {
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    private bool IsTracking => _dragging || _spaceSelecting;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
        }
        else if (e.KeyCode == Keys.ShiftKey && _kind == SelectionKind.Region)
        {
            if (!_slow.IsActive) _slow.Reset(Cursor.Position); // start slow motion from here
        }
        else if (e.KeyCode == Keys.Space && _kind == SelectionKind.Region)
        {
            e.Handled = true;
            if (_spaceKeyDown) return; // auto-repeat
            _spaceKeyDown = true;

            if (_spaceSelecting)
            {
                FinishRegion();                     // 2nd Space: capture
            }
            else if (!_dragging)
            {
                _spaceSelecting = true;             // 1st Space: anchor at the mouse position
                _dragStart = PointToClient(Cursor.Position);
                UpdateRegionTo(_dragStart);
            }
        }
        else if (e.KeyCode == Keys.Enter && !_dragging)
        {
            if (_spaceSelecting) FinishRegion();
            else if (!_selection.IsEmpty) Accept();
        }
    }

    /// <summary>
    /// Arrow keys move the mouse cursor by 1 px (Region mode): before Space they fine-tune the start
    /// point, afterwards (or while dragging) they move the opposite corner of the selection.
    /// Handled here because arrow keys are dialog/navigation keys and never reach OnKeyDown.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_kind == SelectionKind.Region)
        {
            // Shift is the slow-mouse modifier; Shift+arrow still moves 1 px.
            (int dx, int dy) = (keyData & ~Keys.Shift) switch
            {
                Keys.Left => (-1, 0),
                Keys.Right => (1, 0),
                Keys.Up => (0, -1),
                Keys.Down => (0, 1),
                _ => (0, 0),
            };
            if (dx != 0 || dy != 0)
            {
                Point p = Cursor.Position;
                Cursor.Position = new Point(p.X + dx, p.Y + dy); // clamped to the screens by Windows
                if (_slow.IsActive) _slow.Reset(Cursor.Position); // keyboard moves are not slowed
                Point client = PointToClient(Cursor.Position);
                if (IsTracking)
                {
                    UpdateRegionTo(client);
                }
                UpdateMagnifier(client);
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode == Keys.Space) _spaceKeyDown = false;
        if (e.KeyCode == Keys.ShiftKey) _slow.Stop();
    }

    /// <summary>Region selection from the anchor point to <paramref name="p"/> (client coordinates).</summary>
    private void UpdateRegionTo(Point p)
    {
        var r = Rectangle.FromLTRB(
            Math.Min(_dragStart.X, p.X), Math.Min(_dragStart.Y, p.Y),
            Math.Max(_dragStart.X, p.X) + 1, Math.Max(_dragStart.Y, p.Y) + 1);
        SetSelection(Rectangle.Intersect(r, ClientRectangle));
    }

    /// <summary>Ends a Space/drag selection: captures if large enough, otherwise waits for a new one.</summary>
    private void FinishRegion()
    {
        _dragging = false;
        _spaceSelecting = false;
        if (_selection.Width >= 3 && _selection.Height >= 3)
        {
            Accept();
        }
        else
        {
            SetSelection(Rectangle.Empty);
            Invalidate(_hintRect);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right)
        {
            DialogResult = DialogResult.Cancel;
            return;
        }
        if (e.Button == MouseButtons.Left && _kind == SelectionKind.Region)
        {
            if (_spaceSelecting)
            {
                _finishOnMouseUp = true; // finish on button release so the click is not passed to other windows
                return;
            }
            _dragging = true;
            _dragStart = e.Location;
            SetSelection(Rectangle.Empty);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_kind == SelectionKind.Region)
        {
            Point location = ApplySlowMotion(e.Location);
            if (IsTracking)
            {
                UpdateRegionTo(location);
            }
            UpdateMagnifier(location);
        }
        else
        {
            UpdateWindowHighlight(e.Location);
        }
    }

    /// <summary>
    /// While Shift is held, moves the cursor only a quarter (SlowMotion.Factor) of the physical mouse movement.
    /// Returns the (possibly adjusted) cursor position in client coordinates.
    /// </summary>
    private Point ApplySlowMotion(Point client)
    {
        if ((ModifierKeys & Keys.Shift) == 0)
        {
            _slow.Stop(); // Shift released while another window had focus, etc.
            return client;
        }

        Point screen = PointToScreen(client);
        Point target = _slow.Apply(screen);
        if (target == screen) return client;

        Cursor.Position = target;       // generates a WM_MOUSEMOVE echo that Apply() ignores
        Point actual = Cursor.Position; // may be clamped at a screen edge
        _slow.Confirm(actual);
        return PointToClient(actual);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;

        if (_kind == SelectionKind.Region)
        {
            if (_finishOnMouseUp)
            {
                _finishOnMouseUp = false;
                UpdateRegionTo(e.Location);
                FinishRegion();
                return;
            }
            if (!_dragging) return;
            FinishRegion(); // a simple click (too small) keeps waiting
        }
        else if (!_selection.IsEmpty)
        {
            Accept();
        }
    }

    private void UpdateWindowHighlight(Point clientPoint)
    {
        var screenPoint = new Point(clientPoint.X + _virtual.X, clientPoint.Y + _virtual.Y);
        Rectangle highlight = Rectangle.Empty;
        foreach (var w in _windows)
        {
            if (!w.Bounds.Contains(screenPoint)) continue;

            // A point in a window's transparent margin belongs to the window behind it.
            Rectangle bounds = EffectiveBounds(w);
            if (!bounds.Contains(screenPoint)) continue;

            bounds.Offset(-_virtual.X, -_virtual.Y);
            highlight = Rectangle.Intersect(bounds, ClientRectangle);
            break;
        }
        if (highlight != _selection) SetSelection(highlight);
    }

    private Rectangle EffectiveBounds(WindowInfo w)
    {
        if (_resolveBounds == null) return w.Bounds;
        if (!_resolvedBounds.TryGetValue(w.Handle, out Rectangle r))
        {
            r = _resolveBounds(w);
            _resolvedBounds[w.Handle] = r;
        }
        return r;
    }

    private void Accept()
    {
        var r = _selection;
        r.Offset(_virtual.X, _virtual.Y);
        SelectedBounds = r;
        DialogResult = DialogResult.OK;
    }

    private void SetSelection(Rectangle selection)
    {
        _selection = selection;
        Rectangle dirty = DirtyRect(selection);
        Rectangle invalid = _lastDirty.IsEmpty ? dirty : dirty.IsEmpty ? _lastDirty : Rectangle.Union(_lastDirty, dirty);
        if (!invalid.IsEmpty) Invalidate(invalid);
        _lastDirty = dirty;
    }

    private Rectangle DirtyRect(Rectangle sel)
    {
        if (sel.IsEmpty) return Rectangle.Empty;
        var r = Rectangle.Inflate(sel, 4, 4);
        return Rectangle.Union(r, Rectangle.Inflate(LabelRect(sel), 2, 2));
    }

    private string LabelText(Rectangle sel) => $"{sel.Width} × {sel.Height}";

    private Rectangle LabelRect(Rectangle sel)
    {
        Size text = TextRenderer.MeasureText(LabelText(sel), _labelFont);
        int pad = LogicalToDeviceUnits(6);
        var size = new Size(text.Width + pad * 2, text.Height + pad);
        int y = sel.Y - size.Height - LogicalToDeviceUnits(4);
        if (y < 0) y = sel.Y + LogicalToDeviceUnits(4);
        int x = Math.Max(0, sel.X);
        return new Rectangle(x, y, size.Width, size.Height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Rectangle clip = e.ClipRectangle;
        g.CompositingMode = CompositingMode.SourceCopy;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        g.DrawImage(_dimmed, clip, clip, GraphicsUnit.Pixel);

        if (!_selection.IsEmpty)
        {
            Rectangle bright = Rectangle.Intersect(_selection, clip);
            if (!bright.IsEmpty)
                g.DrawImage(_snapshot, bright, bright, GraphicsUnit.Pixel);
        }

        g.CompositingMode = CompositingMode.SourceOver;
        g.PixelOffsetMode = PixelOffsetMode.Default;

        if (!_selection.IsEmpty)
        {
            g.DrawRectangle(_selectionPen, _selection);

            Rectangle label = LabelRect(_selection);
            using (var bg = new SolidBrush(Color.FromArgb(220, 20, 20, 20)))
                g.FillRectangle(bg, label);
            TextRenderer.DrawText(g, LabelText(_selection), _labelFont, label, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        if (!_hintRect.IsEmpty && (_selection.IsEmpty || !_selection.IntersectsWith(_hintRect)) && !IsTracking)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(Color.FromArgb(200, 30, 30, 30)))
            using (var path = RoundedRect(_hintRect, LogicalToDeviceUnits(8)))
                g.FillPath(bg, path);
            TextRenderer.DrawText(g, _hintText, _hintFont, _hintRect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        if (_showMagnifier && !_magRect.IsEmpty && _magRect.IntersectsWith(clip))
        {
            g.SmoothingMode = SmoothingMode.None;
            DrawMagnifier(g);
        }
    }

    // ------------------------------------------------------------------
    //  Magnifier: zoomed view around the cursor, shown at the top-left of the
    //  primary monitor (top-right while the cursor is near the top-left).
    // ------------------------------------------------------------------

    private int MagZoomSize => MagSourceSize * _magCell;

    private int MagInfoHeight => _labelFont.Height * 2 + LogicalToDeviceUnits(10);

    private Rectangle MagRectAt(bool right)
    {
        int margin = LogicalToDeviceUnits(16);
        int w = _magWidth, h = MagZoomSize + MagInfoHeight;
        int x = right ? _primaryClient.Right - margin - w : _primaryClient.X + margin;
        return new Rectangle(x, _primaryClient.Y + margin, w, h);
    }

    private void UpdateMagnifier(Point client)
    {
        if (!_showMagnifier || _primaryClient.IsEmpty) return;

        Rectangle old = _magRect;
        _cursorClient = client;

        // Keep the magnifier away from the cursor and, while selecting, from the selection.
        Rectangle left = MagRectAt(false);
        int keepAway = LogicalToDeviceUnits(40);
        bool right = Rectangle.Inflate(left, keepAway, keepAway).Contains(client)
                     || (IsTracking && _selection.IntersectsWith(left) && !_selection.IntersectsWith(MagRectAt(true)));
        _magRect = right ? MagRectAt(true) : left;

        if (!old.IsEmpty && old != _magRect) Invalidate(old);
        Invalidate(_magRect);
    }

    private void DrawMagnifier(Graphics g)
    {
        int cell = _magCell;
        int half = MagSourceSize / 2;
        var zoom = new Rectangle(_magRect.X + (_magRect.Width - MagZoomSize) / 2, _magRect.Y, MagZoomSize, MagZoomSize);
        var info = new Rectangle(_magRect.X, zoom.Bottom, _magRect.Width, _magRect.Height - MagZoomSize);
        var src = new Rectangle(_cursorClient.X - half, _cursorClient.Y - half, MagSourceSize, MagSourceSize);

        // Zoomed pixels (only the part of the source that lies on the screen).
        g.FillRectangle(Brushes.Black, _magRect.X, _magRect.Y, _magRect.Width, MagZoomSize);
        Rectangle srcOnScreen = Rectangle.Intersect(src, new Rectangle(Point.Empty, _snapshot.Size));
        if (!srcOnScreen.IsEmpty)
        {
            var dest = new Rectangle(
                zoom.X + (srcOnScreen.X - src.X) * cell, zoom.Y + (srcOnScreen.Y - src.Y) * cell,
                srcOnScreen.Width * cell, srcOnScreen.Height * cell);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_snapshot, dest, srcOnScreen, GraphicsUnit.Pixel);
            g.PixelOffsetMode = PixelOffsetMode.Default;
        }

        Region previousClip = g.Clip;
        g.SetClip(zoom);

        // Pixel grid.
        using (var grid = new Pen(Color.FromArgb(40, 255, 255, 255)))
        {
            for (int i = 1; i < MagSourceSize; i++)
            {
                g.DrawLine(grid, zoom.X + i * cell, zoom.Y, zoom.X + i * cell, zoom.Bottom);
                g.DrawLine(grid, zoom.X, zoom.Y + i * cell, zoom.Right, zoom.Y + i * cell);
            }
        }

        // Crosshair through the centre row / column.
        using (var cross = new SolidBrush(Color.FromArgb(70, Theme.Accent)))
        {
            g.FillRectangle(cross, zoom.X, zoom.Y + half * cell, zoom.Width, cell);
            g.FillRectangle(cross, zoom.X + half * cell, zoom.Y, cell, zoom.Height);
        }

        // Current selection edges, mapped into the zoomed view.
        if (!_selection.IsEmpty)
        {
            var sel = new Rectangle(
                zoom.X + (_selection.X - src.X) * cell, zoom.Y + (_selection.Y - src.Y) * cell,
                _selection.Width * cell, _selection.Height * cell);
            using var pen = new Pen(Theme.Accent, Math.Max(2, cell / 4)) { Alignment = PenAlignment.Inset };
            g.DrawRectangle(pen, sel);
        }

        // Centre pixel (the exact start / end point): black + white outline for contrast.
        var center = new Rectangle(zoom.X + half * cell, zoom.Y + half * cell, cell, cell);
        g.DrawRectangle(Pens.Black, center.X - 1, center.Y - 1, center.Width + 1, center.Height + 1);
        g.DrawRectangle(Pens.White, center.X, center.Y, center.Width - 1, center.Height - 1);

        g.Clip = previousClip;
        previousClip.Dispose();

        // Frame.
        using (var frame = new Pen(Theme.Accent, 2) { Alignment = PenAlignment.Inset })
            g.DrawRectangle(frame, _magRect);

        // Info panel: screen coordinates + colour (or selection size while selecting).
        using (var bg = new SolidBrush(Color.FromArgb(235, 20, 20, 20)))
            g.FillRectangle(bg, info);
        int sx = _cursorClient.X + _virtual.X, sy = _cursorClient.Y + _virtual.Y;
        string line1 = $"X {sx}   Y {sy}";
        string line2;
        if (IsTracking && !_selection.IsEmpty)
        {
            line2 = LabelText(_selection);
        }
        else if (new Rectangle(Point.Empty, _snapshot.Size).Contains(_cursorClient))
        {
            Color c = _snapshot.GetPixel(_cursorClient.X, _cursorClient.Y);
            line2 = $"RGB {c.R}, {c.G}, {c.B}   #{c.R:X2}{c.G:X2}{c.B:X2}";
        }
        else
        {
            line2 = string.Empty;
        }
        var lines = Rectangle.Inflate(info, 0, -LogicalToDeviceUnits(4));
        var top = new Rectangle(lines.X, lines.Y, lines.Width, lines.Height / 2);
        var bottom = new Rectangle(lines.X, top.Bottom, lines.Width, lines.Height - top.Height);
        TextRenderer.DrawText(g, line1, _labelFont, top, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, line2, _labelFont, bottom, Color.FromArgb(200, 210, 220),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dimmed.Dispose();
            _labelFont.Dispose();
            _hintFont.Dispose();
            _selectionPen.Dispose();
        }
        base.Dispose(disposing);
    }
}
