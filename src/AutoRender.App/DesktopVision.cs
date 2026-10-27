using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AutoRender.Core;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Rect = System.Windows.Rect;

namespace AutoRender.App;

public sealed record DesktopWindow(IntPtr Handle, int ProcessId, string Id, Rect Bounds);
public sealed record VisionFrame(Rect Bounds, IReadOnlyList<OcrText> Lines, IReadOnlyList<OcrText> Words, byte[] Png)
{
    public void Save(string path) => File.WriteAllBytes(path, Png);
}
public static class DesktopVision
{
    private delegate bool EnumCallback(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint id);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr item);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr item);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr dest, int x, int y, int width, int height, IntPtr src, int sx, int sy, uint operation);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint first, uint count, byte[] bits, ref BitmapInfo info, uint usage);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr window);
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, BitCount;
        public uint Compression, SizeImage; public int XPels, YPels; public uint Used, Important;
    }
    public static IReadOnlyList<DesktopWindow> Windows()
    {
        var ids = Process.GetProcessesByName("CapCut").Select(p => p.Id).ToHashSet();
        var result = new List<DesktopWindow>();
        EnumWindows((handle, _) =>
        {
            GetWindowThreadProcessId(handle, out var pid);
            if (!ids.Contains((int)pid) || !IsWindowVisible(handle)) return true;
            try
            {
                var info = AutomationElement.FromHandle(handle).Current;
                if (info.AutomationId is "HomeWindow" or "MainWindow" or "ExportWindow_Container" &&
                    !info.BoundingRectangle.IsEmpty && info.BoundingRectangle.Width >= 320)
                    result.Add(new(handle, (int)pid, info.AutomationId, info.BoundingRectangle));
            }
            catch (ElementNotAvailableException) { }
            return true;
        }, IntPtr.Zero);
        return result;
    }
    public static DesktopWindow Window(string id)
    {
        var windows = Windows().Where(w => w.Id == id).ToArray();
        if (windows.Length != 1) throw new InvalidOperationException($"Cần một cửa sổ {id}, tìm thấy {windows.Length}.");
        return windows[0];
    }
    public static void Focus(DesktopWindow window)
    {
        if (GetForegroundWindow() != window.Handle && !SetForegroundWindow(window.Handle))
            throw new InvalidOperationException("Không kích hoạt được CapCut. Chuyển sang CapCut rồi thử lại.");
    }
    public static void EnsureForeground(int? expectedPid = null)
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out var pid);
        if (expectedPid != null && pid != expectedPid) throw new InvalidOperationException("Focus đã rời cửa sổ CapCut. Hàng đợi dừng.");
        using var process = Process.GetProcessById((int)pid);
        if (!process.ProcessName.Equals("CapCut", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cửa sổ hiện tại không phải CapCut.");
    }
    private static byte[] CapturePng(Rect requested)
    {
        var desktop = new Rect(GetSystemMetrics(76), GetSystemMetrics(77), GetSystemMetrics(78), GetSystemMetrics(79));
        var rect = Rect.Intersect(desktop, requested);
        if (rect.IsEmpty || rect.Width < requested.Width - 2 || rect.Height < requested.Height - 2)
            throw new InvalidOperationException("Cửa sổ nằm ngoài màn hình. Đưa toàn bộ cửa sổ CapCut vào màn hình.");
        var width = (int)Math.Round(rect.Width); var height = (int)Math.Round(rect.Height);
        var screen = GetDC(IntPtr.Zero); var memory = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, width, height); var previous = SelectObject(memory, bitmap);
        try
        {
            if (screen == IntPtr.Zero || memory == IntPtr.Zero || bitmap == IntPtr.Zero ||
                !BitBlt(memory, 0, 0, width, height, screen, (int)Math.Round(rect.X), (int)Math.Round(rect.Y), 0x40CC0020))
                throw new IOException("Không chụp được màn hình CapCut.");
            SelectObject(memory, previous);
            var pixels = new byte[width * height * 4];
            var info = new BitmapInfo { Size = (uint)Marshal.SizeOf<BitmapInfo>(), Width = width, Height = -height, Planes = 1, BitCount = 32 };
            if (GetDIBits(screen, bitmap, 0, (uint)height, pixels, ref info, 0) != height) throw new IOException("Không đọc được ảnh màn hình.");
            var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, width * 4);
            var scaled = new TransformedBitmap(source, new ScaleTransform(2, 2));
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(scaled));
            using var bytes = new MemoryStream(); encoder.Save(bytes); return bytes.ToArray();
        }
        finally { SelectObject(memory, previous); DeleteObject(bitmap); DeleteDC(memory); ReleaseDC(IntPtr.Zero, screen); }
    }
    public static async Task<VisionFrame> ReadAsync(DesktopWindow window, CancellationToken token, Rect? region = null)
    {
        token.ThrowIfCancellationRequested(); EnsureForeground(window.ProcessId);
        var bounds = region ?? window.Bounds; var png = CapturePng(bounds);
        var available = OcrEngine.AvailableRecognizerLanguages;
        var language = available.FirstOrDefault(l => l.LanguageTag.StartsWith("vi", StringComparison.OrdinalIgnoreCase)) ??
                       available.FirstOrDefault(l => l.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase)) ?? available.FirstOrDefault();
        if (language == null) throw new InvalidOperationException("Windows chưa có OCR. Cài tính năng OCR cho ngôn ngữ tiếng Anh trong Windows Settings > Language > Language options.");
        var engine = OcrEngine.TryCreateFromLanguage(language) ?? throw new InvalidOperationException("Không khởi tạo được Windows OCR.");
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream)) { writer.WriteBytes(png); await writer.StoreAsync(); writer.DetachStream(); }
        stream.Seek(0); var decoder = await global::Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        using var software = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
        if (software.PixelWidth > OcrEngine.MaxImageDimension || software.PixelHeight > OcrEngine.MaxImageDimension)
            throw new InvalidOperationException("Ảnh vượt kích thước Windows OCR hỗ trợ. Thử giảm kích thước cửa sổ.");
        var result = await engine.RecognizeAsync(software); token.ThrowIfCancellationRequested();
        OcrText Convert(string text, Windows.Foundation.Rect box) => new(text, box.X / 2, box.Y / 2, box.Width / 2, box.Height / 2);
        var words = result.Lines.SelectMany(l => l.Words).Select(w => Convert(w.Text, w.BoundingRect)).ToArray();
        var lines = new List<OcrText>();
        foreach (var line in result.Lines)
        {
            var group = new List<OcrText>();
            foreach (var word in line.Words)
            {
                var item = Convert(word.Text, word.BoundingRect);
                if (group.Count > 0 && item.X - (group[^1].X + group[^1].Width) > Math.Max(20, group[^1].Height * 2.5))
                { lines.Add(VisualRules.Merge(group)); group.Clear(); }
                group.Add(item);
            }
            if (group.Count > 0) lines.Add(VisualRules.Merge(group));
        }
        return new(bounds, lines, words, png);
    }
}
