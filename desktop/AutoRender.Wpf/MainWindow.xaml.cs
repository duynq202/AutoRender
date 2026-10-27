using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AutoRender.Wpf;

public class ProjectRow
{
    public string Name { get; set; } = "Project mới";
    public int X { get; set; } = 300;
    public int Y { get; set; } = 300;
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<ProjectRow> Projects { get; } = new();
    private bool isIdle = true;
    public bool IsIdle { get => isIdle; private set { isIdle = value; PropertyChanged?.Invoke(this, new(nameof(IsIdle))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private Process? worker;
    private bool closing;
    private readonly string root;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X; public int Y; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        root = FindRoot();
        ApplyConfig(JsonNode.Parse("""
        {"projects":[{"name":"Project 1","position":[300,300]}],"export_button":[1700,60],"confirm_export_button":[1200,850],"completion_image":"","completion_region":null,"close_completion_button":[1200,850],"home_button":[30,30],"open_delay_seconds":12,"dialog_delay_seconds":3,"home_delay_seconds":5,"export_timeout_seconds":3600,"poll_seconds":2}
        """)!.AsObject());
        Log("Đưa chuột về góc trên bên trái để dừng khẩn cấp. Chạy thử một project trước.");
    }

    private static string FindRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "autorender_worker.py"))) return dir.FullName;
        return Environment.CurrentDirectory;
    }

    private void ApplyConfig(JsonObject config)
    {
        var rows = new List<ProjectRow>();
        foreach (var item in config["projects"]?.AsArray() ?? throw new FormatException("Thiếu projects."))
            rows.Add(new ProjectRow { Name = item!["name"]!.GetValue<string>(),
                X = item["position"]![0]!.GetValue<int>(), Y = item["position"]![1]!.GetValue<int>() });
        config.Remove("projects");
        Projects.Clear();
        foreach (var row in rows) Projects.Add(row);
        SettingsEditor.Text = config.ToJsonString(JsonOptions);
    }

    private JsonObject Config()
    {
        if (!ProjectsGrid.CommitEdit(DataGridEditingUnit.Cell, true) ||
            !ProjectsGrid.CommitEdit(DataGridEditingUnit.Row, true))
            throw new FormatException("Kiểm tra tên và tọa độ project trong bảng.");
        var config = JsonNode.Parse(SettingsEditor.Text)?.AsObject() ?? throw new FormatException("JSON không hợp lệ.");
        var projects = new JsonArray();
        foreach (var row in Projects)
        {
            if (string.IsNullOrWhiteSpace(row.Name) || row.X < 0 || row.Y < 0)
                throw new FormatException("Project cần tên và tọa độ không âm.");
            projects.Add(new JsonObject { ["name"] = row.Name, ["position"] = new JsonArray(row.X, row.Y) });
        }
        if (projects.Count == 0) throw new FormatException("Thêm ít nhất một project.");
        config["projects"] = projects;
        return config;
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Cấu hình JSON|*.json" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var config = JsonNode.Parse(File.ReadAllText(dialog.FileName))!.AsObject();
            var image = config["completion_image"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(image) && !Path.IsPathRooted(image))
                config["completion_image"] = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dialog.FileName)!, image));
            ApplyConfig(config);
        }
        catch (Exception ex) { Error(ex); }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var config = Config();
            var dialog = new SaveFileDialog { Filter = "Cấu hình JSON|*.json", FileName = "config.local.json" };
            if (dialog.ShowDialog() == true) File.WriteAllText(dialog.FileName, config.ToJsonString(JsonOptions));
        }
        catch (Exception ex) { Error(ex); }
    }

    private void Image_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Ảnh PNG|*.png" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var settings = JsonNode.Parse(SettingsEditor.Text)!.AsObject();
            settings["completion_image"] = dialog.FileName;
            SettingsEditor.Text = settings.ToJsonString(JsonOptions);
        }
        catch (Exception ex) { Error(ex); }
    }

    private async void Capture_Click(object sender, RoutedEventArgs e)
    {
        CaptureButton.IsEnabled = false;
        try
        {
            for (var remaining = 5; remaining > 0; remaining--)
            {
                StatusText.Text = $"Lấy tọa độ sau {remaining}s — đưa chuột đến vị trí cần lấy";
                await Task.Delay(1000);
            }
            if (!GetCursorPos(out var point)) throw new InvalidOperationException("Không đọc được vị trí chuột.");
            StatusText.Text = $"Tọa độ: [{point.X}, {point.Y}]";
            Log(StatusText.Text);
        }
        catch (Exception ex) { Error(ex); }
        finally { CaptureButton.IsEnabled = true; }
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!IsIdle || !CaptureButton.IsEnabled) return;
        string? configPath = null;
        try
        {
            var config = Config();
            var python = Path.Combine(root, ".venv", "Scripts", "python.exe");
            if (!File.Exists(python)) throw new FileNotFoundException("Chạy start-wpf.bat để cài Python backend trước.");
            configPath = Path.Combine(Path.GetTempPath(), $"autorender-{Guid.NewGuid():N}.json");
            File.WriteAllText(configPath, config.ToJsonString(JsonOptions));
            var info = new ProcessStartInfo(python) {
                WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            info.ArgumentList.Add("-u");
            info.ArgumentList.Add(Path.Combine(root, "autorender_worker.py"));
            info.ArgumentList.Add(configPath);
            info.Environment["PYTHONIOENCODING"] = "utf-8";
            using var process = new Process { StartInfo = info };
            IsIdle = false;
            StopButton.IsEnabled = true;
            StatusText.Text = "Đang chạy · Chuyển sang trang chủ CapCut";
            if (!process.Start()) throw new InvalidOperationException("Không khởi động được backend.");
            worker = process;
            var stdout = ReadLog(process.StandardOutput);
            var stderr = ReadLog(process.StandardError);
            await process.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);
            StatusText.Text = process.ExitCode == 0 ? "Hoàn tất · Kiểm tra file video đầu ra" :
                process.ExitCode == 2 ? "Đã dừng · Kiểm tra trạng thái CapCut" : "Có lỗi · Xem nhật ký";
        }
        catch (Exception ex) { Error(ex); StatusText.Text = "Không thể chạy"; }
        finally
        {
            worker = null;
            IsIdle = true;
            StopButton.IsEnabled = false;
            if (configPath != null) { try { File.Delete(configPath); } catch (IOException) { } }
            if (closing) Close();
        }
    }

    private async Task ReadLog(StreamReader reader)
    {
        while (await reader.ReadLineAsync() is { } line) Log(line);
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => Stop();
    private void Stop()
    {
        try
        {
            if (worker is { HasExited: false })
            {
                worker.StandardInput.WriteLine("stop");
                worker.StandardInput.Flush();
                StatusText.Text = "Đang dừng · Không hủy tác vụ xuất trong CapCut";
            }
        }
        catch (IOException ex) { Log(ex.Message); }
        catch (InvalidOperationException ex) { Log(ex.Message); }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!IsIdle) { e.Cancel = true; closing = true; Stop(); }
    }

    private void Log(string message)
    {
        LogBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }
    private void Error(Exception ex) => MessageBox.Show(this, ex.Message, "AutoRender", MessageBoxButton.OK, MessageBoxImage.Warning);
}
