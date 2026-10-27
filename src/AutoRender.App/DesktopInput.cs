using System.Runtime.InteropServices;
using System.Windows;
using AutoRender.Core;
namespace AutoRender.App;

public static class DesktopInput
{
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Mouse { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct Keyboard { public ushort Key, Scan; public uint Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Explicit)] private struct InputData { [FieldOffset(0)] public Mouse Mouse; [FieldOffset(0)] public Keyboard Keyboard; }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputData Data; }
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    public static void Guard(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (GetCursorPos(out var point) && point.X <= 1 && point.Y <= 1)
            throw new OperationCanceledException("Dừng khẩn cấp: chuột ở góc trên trái.", token);
        DesktopVision.EnsureForeground();
    }
    private static void Send(Input[] inputs)
    {
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            throw new InvalidOperationException("Windows không nhận đủ thao tác. Kiểm tra CapCut và AutoRender chạy cùng mức quyền.");
    }
    private static Input Key(ushort key, bool up = false) => new() { Type = 1, Data = new() { Keyboard = new() { Key = key, Flags = up ? 2u : 0u } } };
    public static void Shortcut(ushort modifier, ushort key, CancellationToken token)
    { Guard(token); Send([Key(modifier), Key(key), Key(key, true), Key(modifier, true)]); }
    public static void Move(double x, double y, CancellationToken token)
    {
        Guard(token);
        if (!SetCursorPos((int)Math.Round(x), (int)Math.Round(y))) throw new InvalidOperationException("Không di chuyển được chuột.");
    }
    public static void Click(double x, double y, CancellationToken token)
    {
        Move(x, y, token);
        Send([new() { Type = 0, Data = new() { Mouse = new() { Flags = 2 } } }, new() { Type = 0, Data = new() { Mouse = new() { Flags = 4 } } }]);
    }
    public static void Scroll(int notches, CancellationToken token)
    { Guard(token); Send([new() { Type = 0, Data = new() { Mouse = new() { Flags = 0x800, Data = unchecked((uint)(notches * 120)) } } }]); }
    public static void Type(string text, CancellationToken token)
    {
        Guard(token);
        var inputs = new List<Input>();
        foreach (var c in text)
        {
            inputs.Add(new() { Type = 1, Data = new() { Keyboard = new() { Scan = c, Flags = 4 } } });
            inputs.Add(new() { Type = 1, Data = new() { Keyboard = new() { Scan = c, Flags = 6 } } });
        }
        Send(inputs.ToArray());
    }
    public static async Task FillVerified(double x, double y, string text, bool path, CancellationToken token)
    {
        Click(x, y, token); Shortcut(0x11, 0x41, token); Type(text, token);
        // Copy only the targeted CapCut field. Never log clipboard contents.
        System.Windows.Application.Current.Dispatcher.Invoke(() => Clipboard.SetText("AutoRender-field-readback-" + Guid.NewGuid().ToString("N")));
        Shortcut(0x11, 0x41, token); Shortcut(0x11, 0x43, token);
        await Task.Delay(250, token);
        var actual = System.Windows.Application.Current.Dispatcher.Invoke(() => Clipboard.GetText());
        if (!VisualRules.ValueMatches(actual, text, path))
            throw new InvalidOperationException("Không đọc lại được chính xác giá trị đã nhập trong CapCut. Đã dừng, không nhấn Xuất.");
    }
}
