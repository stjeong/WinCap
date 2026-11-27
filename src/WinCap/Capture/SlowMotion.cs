namespace WinCap.Capture;

/// <summary>
/// Precision mode for the mouse: while active, the cursor only travels a fraction
/// (<see cref="Factor"/>) of the physical mouse movement.
/// </summary>
/// <remarks>
/// The caller feeds every observed cursor position (screen coordinates) to <see cref="Apply"/>
/// and moves the real cursor to the returned position. Fractions are accumulated so slow,
/// 1-px movements are not lost to rounding. Positions the caller set itself (the WM_MOUSEMOVE
/// echo of SetCursorPos, or arrow-key moves reported via <see cref="Reset"/>) are not scaled again.
/// </remarks>
internal sealed class SlowMotion
{
    public const float Factor = 0.25f; // quarter of the physical mouse movement

    private Point? _last;      // last position the cursor was set to (or observed when starting)
    private PointF _precise;   // accumulated position including fractions

    public bool IsActive => _last.HasValue;

    /// <summary>Starts (or re-bases) slow motion at the given cursor position.</summary>
    public void Reset(Point screen)
    {
        _last = screen;
        _precise = screen;
    }

    public void Stop() => _last = null;

    /// <summary>
    /// Returns where the cursor should be for the observed position <paramref name="screen"/>.
    /// Call <see cref="Confirm"/> with the position the cursor actually ended up at (it may be clamped).
    /// </summary>
    public Point Apply(Point screen)
    {
        if (_last is not Point last)
        {
            Reset(screen);
            return screen;
        }
        if (screen == last)
        {
            return screen; // echo of our own SetCursorPos, or no movement
        }

        _precise = new PointF(
            _precise.X + (screen.X - last.X) * Factor,
            _precise.Y + (screen.Y - last.Y) * Factor);
        var target = new Point((int)MathF.Round(_precise.X), (int)MathF.Round(_precise.Y));
        _last = target;
        return target;
    }

    /// <summary>Records where the cursor really is after being moved to the result of <see cref="Apply"/>.</summary>
    public void Confirm(Point actual)
    {
        if (_last is Point last && actual != last)
        {
            // Clamped at a screen edge: continue from the real position.
            Reset(actual);
        }
    }
}
