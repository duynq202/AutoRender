using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;
using AutoRender.Core;

namespace AutoRender.App;

public sealed class CapCutDriver(string executable, UiProfile profile, Action<string> log) : ICapCutDriver
{
    private AutomationElement? root;
    private IReadOnlyList<WindowCandidate> windowCandidates = [];
    private WindowCandidate? selectedWindow;
    private delegate bool EnumWindowCallback(IntPtr handle, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out NativeRect rectangle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr handle, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr handle, StringBuilder text, int max);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr handle, int index);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr handle, uint command);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }

    private static IReadOnlyList<WindowCandidate> EnumerateWindows()
    {
        var processIds = Process.GetProcessesByName("CapCut").Select(p => p.Id).ToHashSet();
        var windows = new List<WindowCandidate>();
        if (!EnumWindows((handle, _) =>
        {
            GetWindowThreadProcessId(handle, out var processId);
            if (!processIds.Contains((int)processId)) return true;
            var title = new StringBuilder(1024); var className = new StringBuilder(512);
            GetWindowText(handle, title, title.Capacity); GetClassName(handle, className, className.Capacity);
            if (!GetWindowRect(handle, out var bounds)) return true;
            windows.Add(new(handle.ToInt64(), (int)processId, className.ToString(), title.ToString(),
                IsWindowVisible(handle), (GetWindowLongPtr(handle, -20).ToInt64() & 0x80) != 0,
                GetWindow(handle, 4) != IntPtr.Zero, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top));
            return true;
        }, IntPtr.Zero)) throw new InvalidOperationException("Không liệt kê được cửa sổ Windows.");
        return windows;
    }

    private TimeSpan openTimeout = TimeSpan.FromMinutes(2);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X; public int Y; }

    private void Safety(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (GetCursorPos(out var point) && point.X <= 1 && point.Y <= 1)
            throw new OperationCanceledException("Chuột ở góc trên trái: đã kích hoạt dừng khẩn cấp.", token);
    }

    public async Task ConnectAsync(CancellationToken token)
    {
        if (Process.GetProcessesByName("CapCut").Length == 0)
        {
            if (!File.Exists(executable) || !Path.GetFileName(executable).Equals("CapCut.exe", StringComparison.OrdinalIgnoreCase))
                throw new FileNotFoundException("Chọn CapCut.exe để mở CapCut.");
            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
        }
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(90))
        {
            Safety(token);
            windowCandidates = EnumerateWindows();
            selectedWindow = WindowPicker.Choose(windowCandidates, GetForegroundWindow().ToInt64());
            if (selectedWindow != null)
            {
                using var process = Process.GetProcessById(selectedWindow.ProcessId);
                var version = FileVersionInfo.GetVersionInfo(process.MainModule!.FileName).ProductVersion ?? "";
                if (!version.StartsWith(profile.Version, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"CapCut {version} không khớp profile {profile.Version}.");
                var handle = new IntPtr(selectedWindow.Handle);
                // Foreground activation is best effort. Inspection does not require stealing focus.
                if (!SetForegroundWindow(handle)) log("Không chuyển được focus; tiếp tục đọc cây UI của cửa sổ đã xác định.");
                root = AutomationElement.FromHandle(handle);
                log($"Đã kết nối CapCut {version}: {selectedWindow.ClassName}, {selectedWindow.Width} × {selectedWindow.Height}.");
                return;
            }
            await Task.Delay(500, token);
        }
        var details = string.Join("; ", windowCandidates.Select(w => $"{w.ClassName} {w.Width}x{w.Height} visible={w.Visible} tool={w.ToolWindow}"));
        throw new TimeoutException($"Không thấy cửa sổ ứng dụng CapCut phù hợp. Bỏ qua tooltip/cửa sổ nhỏ. Các cửa sổ tìm thấy: {details}");
    }

    private static Condition ConditionFor(Selector selector)
    {
        var conditions = new List<Condition>();
        if (!string.IsNullOrEmpty(selector.Name)) conditions.Add(new PropertyCondition(AutomationElement.NameProperty, selector.Name));
        if (!string.IsNullOrEmpty(selector.AutomationId)) conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, selector.AutomationId));
        if (!string.IsNullOrEmpty(selector.ControlType))
        {
            var field = typeof(ControlType).GetField(selector.ControlType);
            if (field?.GetValue(null) is not ControlType type) throw new InvalidDataException($"ControlType không hợp lệ: {selector.ControlType}");
            conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, type));
        }
        if (conditions.Count == 0) throw new InvalidDataException("Không được dùng selector rỗng.");
        return conditions.Count == 1 ? conditions[0] : new AndCondition(conditions.ToArray());
    }
    private AutomationElement? Find(Selector selector, AutomationElement? scope = null)
    {
        var matches = (scope ?? root ?? throw new InvalidOperationException("Chưa kết nối CapCut."))
            .FindAll(TreeScope.Descendants, ConditionFor(selector));
        var visible = matches.Cast<AutomationElement>().Where(e => !e.Current.IsOffscreen).ToArray();
        if (visible.Length > 1) throw new InvalidOperationException($"Selector không duy nhất: {selector}. Có {visible.Length} kết quả.");
        return visible.SingleOrDefault();
    }
    private Selector Control(string key) => profile.Controls.TryGetValue(key, out var s) && !s.IsEmpty ? s
        : throw new InvalidDataException($"Thiếu selector {key}.");
    private async Task<AutomationElement> Wait(Selector selector, CancellationToken token, AutomationElement? scope = null)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < openTimeout)
        {
            Safety(token);
            var element = Find(selector, scope);
            if (element != null && element.Current.IsEnabled) return element;
            await Task.Delay(500, token);
        }
        throw new TimeoutException($"Không nhận diện được phần tử: {selector}");
    }
    private void Invoke(AutomationElement element, CancellationToken token)
    {
        Safety(token);
        if (!element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
            throw new NotSupportedException($"Phần tử '{element.Current.Name}' không hỗ trợ Invoke. Cần khảo sát lại profile hoặc phương án ảnh.");
        ((InvokePattern)pattern).Invoke();
    }
    private void SetText(AutomationElement element, string text, CancellationToken token)
    {
        Safety(token);
        if (!element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern) || ((ValuePattern)pattern).Current.IsReadOnly)
            throw new NotSupportedException($"Ô '{element.Current.Name}' không cho nhập qua UI Automation.");
        var value = (ValuePattern)pattern; value.SetValue(text);
        if (!string.Equals(value.Current.Value, text, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Không xác nhận được giá trị đã nhập: {element.Current.Name}");
    }
    private async Task Select(AutomationElement combo, string label, CancellationToken token)
    {
        Safety(token);
        if (combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expand)) ((ExpandCollapsePattern)expand).Expand();
        else Invoke(combo, token);
        var option = await Wait(new Selector(Name: label), token);
        if (!option.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var select))
            throw new NotSupportedException($"Mục '{label}' không hỗ trợ SelectionItem.");
        ((SelectionItemPattern)select).Select();
        Safety(token);
        if (!((SelectionItemPattern)select).Current.IsSelected)
            throw new InvalidOperationException($"Không xác nhận được lựa chọn: {label}");
        if (expand != null) ((ExpandCollapsePattern)expand).Collapse();
    }
    public async Task OpenProjectAsync(string name, ExportSettings settings, CancellationToken token)
    {
        openTimeout = TimeSpan.FromSeconds(settings.OpenTimeoutSeconds);
        var search = await Wait(Control("search"), token);
        SetText(search, name, token);
        await Task.Delay(1000, token);
        var card = await Wait(new Selector(Name: name, ControlType: profile.ProjectControlType), token);
        // An exact project selector must target an invokable card, not a guessed screen coordinate.
        Invoke(card, token);
        await Wait(Control("editorMarker"), token);
        await Wait(new Selector(Name: name), token);
    }
    public async Task ConfigureAsync(string videoName, ExportSettings settings, CancellationToken token)
    {
        Invoke(await Wait(Control("openExport"), token), token);
        var dialog = await Wait(Control("dialog"), token);
        SetText(await Wait(Control("fileName"), token, dialog), videoName + (profile.FileNameIncludesExtension ? ".mp4" : ""), token);
        SetText(await Wait(Control("folder"), token, dialog), settings.OutputDirectory, token);
        await Select(await Wait(Control("resolution"), token, dialog), profile.ResolutionLabels[settings.Resolution], token);
        await Select(await Wait(Control("fps"), token, dialog), profile.FpsLabels[settings.Fps.ToString()], token);
        await Select(await Wait(Control("format"), token, dialog), profile.Mp4Label, token);
        if (Find(Control("success")) != null) throw new InvalidOperationException("Thông báo thành công cũ đang hiện. Dừng để tránh nhận nhầm.");
    }
    public async Task BeginExportAsync(CancellationToken token)
    {
        var dialog = await Wait(Control("dialog"), token);
        Invoke(await Wait(Control("confirmExport"), token, dialog), token);
    }
    public Task<bool> IsExportCompleteAsync(CancellationToken token)
    {
        Safety(token); return Task.FromResult(Find(Control("success")) != null);
    }
    public async Task ReturnHomeAsync(CancellationToken token)
    {
        Invoke(await Wait(Control("closeSuccess"), token), token);
        Invoke(await Wait(Control("home"), token), token);
        await Wait(Control("search"), token);
    }
    public void Dump(string path, CancellationToken token)
    {
        var nodes = new List<object>(); var truncated = false;
        void Walk(AutomationElement element, int depth)
        {
            Safety(token);
            if (nodes.Count >= 5000 || depth > 25) { truncated = true; return; }
            try
            {
                var data = element.Current;
                nodes.Add(new { Depth = depth, data.Name, data.AutomationId, Type = data.ControlType.ProgrammaticName,
                    data.ClassName, data.IsOffscreen, Bounds = data.BoundingRectangle.ToString(),
                    Patterns = element.GetSupportedPatterns().Select(p => p.ProgrammaticName).ToArray() });
                var child = TreeWalker.RawViewWalker.GetFirstChild(element);
                while (child != null && nodes.Count < 5000) { Walk(child, depth + 1); child = TreeWalker.RawViewWalker.GetNextSibling(child); }
                if (child != null) truncated = true;
            }
            catch (ElementNotAvailableException) { truncated = true; }
        }
        if (root == null) throw new InvalidOperationException("Chưa kết nối.");
        // Inspect every substantial CapCut top-level window, including separate export dialogs.
        windowCandidates = EnumerateWindows();
        var inspected = new List<long>();
        foreach (var window in windowCandidates.Where(w => w.IsApplicationWindow))
        {
            if (nodes.Count >= 5000) { truncated = true; break; }
            try { Walk(AutomationElement.FromHandle(new IntPtr(window.Handle)), 0); inspected.Add(window.Handle); }
            catch (ElementNotAvailableException) { truncated = true; }
        }
        if (inspected.Count == 0) throw new InvalidOperationException("Cửa sổ CapCut đã biến mất. Hãy khảo sát lại.");
        File.WriteAllText(path, JsonSerializer.Serialize(new { SchemaVersion = 2, Version = profile.Version,
            SelectedWindow = selectedWindow, WindowCandidates = windowCandidates, InspectedHandles = inspected,
            Truncated = truncated, Nodes = nodes }, new JsonSerializerOptions { WriteIndented = true }));
        if (nodes.Count <= inspected.Count) log("Chỉ đọc được các node cửa sổ. Chưa thể xác nhận UI Automation truy cập được các control bên trong.");
    }
}
