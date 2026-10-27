namespace AutoRender.Core;

public record WindowCandidate(long Handle, int ProcessId, string ClassName, string Title,
    bool Visible, bool ToolWindow, bool Owned, int Width, int Height)
{
    public bool IsApplicationWindow => Visible && !ToolWindow && Width >= 320 && Height >= 200 &&
        !ClassName.Contains("tooltip", StringComparison.OrdinalIgnoreCase);
    public long Area => (long)Width * Height;
}

public static class WindowPicker
{
    public static WindowCandidate? Choose(IReadOnlyList<WindowCandidate> windows, long foreground)
    {
        var candidates = windows.Where(w => w.IsApplicationWindow).ToArray();
        if (candidates.FirstOrDefault(w => w.Handle == foreground) is { } active) return active;
        // A tooltip returned as Process.MainWindowHandle must not hide the real Qt/QML window.
        var unowned = candidates.Where(w => !w.Owned).ToArray();
        var pool = unowned.Length > 0 ? unowned : candidates;
        var sorted = pool.OrderByDescending(w => w.Area).ToArray();
        if (sorted.Length > 1 && sorted[0].Area == sorted[1].Area)
            throw new InvalidOperationException("Có nhiều cửa sổ CapCut cùng kích thước. Đóng bớt cửa sổ rồi khảo sát lại.");
        return sorted.FirstOrDefault();
    }
}
