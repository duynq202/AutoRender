using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using AutoRender.Core;
using Rect = System.Windows.Rect;
namespace AutoRender.App;

/// <summary>Experimental adapter based on the user's CapCut 9.6 Vietnamese screenshots.</summary>
public sealed class VisualCapCutDriver(string executable, UiProfile profile, Action<string> log, string diagnostics) : ICapCutDriver
{
    private TimeSpan openTimeout = TimeSpan.FromSeconds(120);
    private VisionFrame? lastFrame;
    private readonly Stopwatch exportTransition = new();
    private string projectName = "";
    private async Task<VisionFrame> Read(string id, CancellationToken token)
    {
        var window = DesktopVision.Window(id);
        lastFrame = await DesktopVision.ReadAsync(window, token); return lastFrame;
    }
    private void Snapshot(string stage)
    {
        if (lastFrame == null) return;
        Directory.CreateDirectory(diagnostics);
        var prefix = Path.Combine(diagnostics, $"{DateTime.Now:yyyyMMdd-HHmmssfff}-{stage}");
        lastFrame.Save(prefix + ".png");
        File.WriteAllLines(prefix + ".txt", lastFrame.Lines.Select(t => $"{t.X:F0},{t.Y:F0} {t.Text}"));
        log("Đã lưu ảnh/OCR chẩn đoán: " + prefix);
    }
    private async Task<T> Checked<T>(string stage, Func<Task<T>> action)
    {
        try { return await action(); }
        catch { try { Snapshot(stage); } catch (IOException) { } throw; }
    }
    public async Task ConnectAsync(CancellationToken token)
    {
        // Keep the tested window/version discovery, then use only window-level UIA metadata.
        await new CapCutDriver(executable, profile, log).ConnectAsync(token);
        var windows = DesktopVision.Windows();
        if (windows.Count == 0) throw new InvalidOperationException("Không nhận diện được cửa sổ CapCut qua AutomationId.");
        var target = windows.FirstOrDefault(w => w.Id == "HomeWindow") ?? windows.FirstOrDefault(w => w.Id == "MainWindow") ?? windows[0];
        DesktopVision.Focus(target);
        log("Chế độ OCR thử nghiệm: không dùng selector nút UI Automation. Hãy thử một project ngắn trước.");
    }
    private async Task<DesktopWindow> WaitWindow(string id, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < openTimeout)
        {
            DesktopInput.Guard(token);
            var windows = DesktopVision.Windows().Where(w => w.Id == id).ToArray();
            if (windows.Length > 1) throw new InvalidOperationException($"Nhiều cửa sổ {id} trùng nhau.");
            if (windows.Length == 1) return windows[0];
            await Task.Delay(500, token);
        }
        throw new TimeoutException($"Chờ cửa sổ {id} quá lâu.");
    }
    private static void Click(VisionFrame frame, OcrText text, CancellationToken token)
        => DesktopInput.Click(frame.Bounds.X + text.CenterX, frame.Bounds.Y + text.CenterY, token);
    private static OcrText Label(VisionFrame frame, string label)
        => VisualRules.Unique(VisualRules.Rows(frame.Words.Where(t => t.X > frame.Bounds.Width * .45 && t.X < frame.Bounds.Width * .63)), label);

    public async Task OpenProjectAsync(string name, ExportSettings settings, CancellationToken token)
    {
        await Checked("open-project", async () =>
        {
            projectName = name; openTimeout = TimeSpan.FromSeconds(settings.OpenTimeoutSeconds);
            var home = await WaitWindow("HomeWindow", token); DesktopVision.Focus(home);
            var scale = DesktopVision.GetDpiForWindow(home.Handle) / 96.0;
            VisionFrame frame = await Read("HomeWindow", token);
            OcrText? header = null;
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var headers = frame.Lines.Where(t => VisualRules.Key(t.Text) == "duan" && t.Y > frame.Bounds.Height * .35).ToArray();
                if (headers.Length == 1) { header = headers[0]; break; }
                if (headers.Length > 1) throw new InvalidOperationException("OCR nhận diện nhiều tiêu đề Dự án.");
                DesktopInput.Click(frame.Bounds.X + frame.Bounds.Width * .6, frame.Bounds.Y + frame.Bounds.Height * .6, token);
                DesktopInput.Scroll(-3, token); await Task.Delay(600, token); frame = await Read("HomeWindow", token);
            }
            if (header == null) throw new InvalidOperationException("Không đọc được tiêu đề Dự án. Dùng Kiểm tra OCR và gửi ảnh chẩn đoán.");
            // Home screenshot: search icon is 336 logical pixels from the right window edge,
            // aligned to the project section header. Bounds/DPI are read live; no desktop coordinates.
            DesktopInput.Click(home.Bounds.Right - 336 * scale, home.Bounds.Top + header.CenterY, token);
            await Task.Delay(350, token);
            DesktopInput.Shortcut(0x11, 0x41, token); DesktopInput.Type(name, token);
            await Task.Delay(1000, token);
            frame = await Read("HomeWindow", token);
            var card = VisualRules.Unique(frame.Lines, name, t => t.Y > header.CenterY + 24 * scale && t.X > frame.Bounds.Width * .14);
            // Double-click thumbnail above the title; double-clicking the title can rename it.
            var x = frame.Bounds.X + card.CenterX;
            var y = frame.Bounds.Y + card.Y - 45 * scale;
            if (y <= frame.Bounds.Top + header.CenterY) throw new InvalidOperationException("Vị trí thumbnail không hợp lệ.");
            DesktopInput.Click(x, y, token); await Task.Delay(100, token); DesktopInput.Click(x, y, token);
            await WaitWindow("MainWindow", token);
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < openTimeout)
            {
                frame = await Read("MainWindow", token);
                var titles = frame.Lines.Where(t => t.Y < 50 * scale && VisualRules.Key(t.Text) == VisualRules.Key(name)).ToArray();
                if (titles.Length == 1) { log("Đã xác nhận project: " + name); return true; }
                await Task.Delay(500, token);
            }
            throw new InvalidOperationException("Đã mở editor nhưng OCR không xác nhận được đúng tên project.");
        });
    }
    private async Task SelectSetting(string label, string expected, CancellationToken token)
    {
        var frame = await Read("ExportWindow_Container", token);
        var anchor = Label(frame, label);
        var scale = DesktopVision.GetDpiForWindow(DesktopVision.Window("ExportWindow_Container").Handle) / 96.0;
        var values = VisualRules.Rows(frame.Words.Where(t => t.X > frame.Bounds.Width * .63 && t.X < frame.Bounds.Width * .93 && Math.Abs(t.CenterY - anchor.CenterY) < 15 * scale)).ToArray();
        if (values.Any(t => VisualRules.Key(t.Text) == VisualRules.Key(expected))) return;
        DesktopInput.Click(frame.Bounds.X + frame.Bounds.Width * .80, frame.Bounds.Y + anchor.CenterY, token);
        await Task.Delay(400, token);
        var window = DesktopVision.Window("ExportWindow_Container");
        var region = new Rect(window.Bounds.X + window.Bounds.Width * .63, Math.Max(window.Bounds.Y, window.Bounds.Y + anchor.CenterY - 200 * scale),
            window.Bounds.Width * .36, Math.Min(450 * scale, window.Bounds.Bottom - Math.Max(window.Bounds.Y, window.Bounds.Y + anchor.CenterY - 200 * scale)));
        var menu = await DesktopVision.ReadAsync(window, token, region); lastFrame = menu;
        OcrText option;
        var exactLines = menu.Lines.Where(t => VisualRules.Key(t.Text) == VisualRules.Key(expected)).ToArray();
        if (exactLines.Length == 1) option = exactLines[0];
        else option = VisualRules.Unique(menu.Words, expected);
        Click(menu, option, token); await Task.Delay(350, token);
        frame = await Read("ExportWindow_Container", token); anchor = Label(frame, label);
        if (!VisualRules.Rows(frame.Words.Where(t => t.X > frame.Bounds.Width * .63 && t.X < frame.Bounds.Width * .93 && Math.Abs(t.CenterY - anchor.CenterY) < 15 * scale))
            .Any(t => VisualRules.Key(t.Text) == VisualRules.Key(expected)))
            throw new InvalidOperationException($"Không xác nhận được {label} = {expected} sau khi chọn.");
    }
    private async Task Fill(string label, string value, bool path, CancellationToken token)
    {
        var frame = await Read("ExportWindow_Container", token); var anchor = Label(frame, label);
        await DesktopInput.FillVerified(frame.Bounds.X + frame.Bounds.Width * .80, frame.Bounds.Y + anchor.CenterY, value, path, token);
    }
    public async Task ConfigureAsync(string videoName, ExportSettings settings, CancellationToken token)
    {
        await Checked("configure-export", async () =>
        {
            var editor = await Read("MainWindow", token);
            var export = VisualRules.Unique(editor.Lines, "Xuất", t => t.X > editor.Bounds.Width * .8 && t.Y < 60);
            Click(editor, export, token);
            await WaitWindow("ExportWindow_Container", token); await Task.Delay(500, token);
            var frame = await Read("ExportWindow_Container", token);
            if (VisualRules.IsSuccess(frame.Lines)) throw new InvalidOperationException("Thông báo thành công cũ đang mở.");
            await Fill("Tên", videoName, false, token);
            // The supplied UI shows a FULL output file path, not just a directory.
            var output = Path.Combine(settings.OutputDirectory, videoName + ".mp4");
            await Fill("Xuất sang", output, true, token);
            await SelectSetting("Độ phân giải", settings.Resolution, token);
            await SelectSetting("Tỷ lệ khung hình", settings.Fps + "fps", token);
            await SelectSetting("Định dạng", "mp4", token);
            // Recheck editable fields after controls may have committed/reformatted them.
            await Fill("Tên", videoName, false, token);
            await Fill("Xuất sang", output, true, token);
            log($"Đã đọc lại tên/đường dẫn và xác nhận {settings.Resolution}, {settings.Fps} FPS, MP4.");
            return true;
        });
    }
    public async Task BeginExportAsync(CancellationToken token)
    {
        await Checked("begin-export", async () =>
        {
            var frame = await Read("ExportWindow_Container", token);
            var export = VisualRules.Unique(frame.Lines, "Xuất", t => t.X > frame.Bounds.Width * .65 && t.Y > frame.Bounds.Height * .85);
            Click(frame, export, token); exportTransition.Restart(); return true;
        });
    }
    public async Task<bool> IsExportCompleteAsync(CancellationToken token)
    {
        var exports = DesktopVision.Windows().Where(w => w.Id == "ExportWindow_Container").ToArray();
        if (exports.Length == 0 && exportTransition.Elapsed < openTimeout) return false;
        if (exports.Length != 1) throw new InvalidOperationException("Không nhận diện được một cửa sổ xuất CapCut. Kiểm tra trạng thái ứng dụng.");
        var frame = await Read("ExportWindow_Container", token); return VisualRules.IsSuccess(frame.Lines);
    }
    public async Task ReturnHomeAsync(CancellationToken token)
    {
        await Checked("return-home", async () =>
        {
            var success = await Read("ExportWindow_Container", token);
            if (!VisualRules.IsSuccess(success.Lines)) throw new InvalidOperationException("Trạng thái thành công đã thay đổi. Không tự đóng cửa sổ.");
            var close = VisualRules.Unique(success.Lines, "Đóng", t => t.Y > success.Bounds.Height * .85);
            Click(success, close, token); await Task.Delay(500, token);
            var editor = await WaitWindow("MainWindow", token);
            var frame = await Read("MainWindow", token);
            VisualRules.Unique(frame.Lines, projectName, t => t.Y < 60);
            var element = AutomationElement.FromHandle(editor.Handle);
            if (!element.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
                throw new InvalidOperationException("Editor không hỗ trợ đóng cửa sổ để về trang chủ.");
            DesktopInput.Guard(token); ((WindowPattern)pattern).Close();
            var home = await WaitWindow("HomeWindow", token); DesktopVision.Focus(home); return true;
        });
    }
    public async Task InspectAsync(CancellationToken token)
    {
        var window = DesktopVision.Windows().FirstOrDefault(w => w.Id == "ExportWindow_Container") ??
                     DesktopVision.Windows().FirstOrDefault(w => w.Id == "HomeWindow") ?? DesktopVision.Window("MainWindow");
        DesktopVision.Focus(window); lastFrame = await DesktopVision.ReadAsync(window, token); Snapshot("ocr-check");
        log($"OCR: {lastFrame.Lines.Count} dòng trên {window.Id}.");
        foreach (var line in lastFrame.Lines) log($"[{line.X:F0},{line.Y:F0}] {line.Text}");
    }
}
