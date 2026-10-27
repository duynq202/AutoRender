using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AutoRender.Core;
using Microsoft.Win32;

namespace AutoRender.App;
public partial class MainWindow : Window
{
    private readonly string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoRenderNative");
    private readonly JobStore store;
    private CancellationTokenSource? cancellation;
    private bool busy, closeAfterStop;
    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(data);
        store = new JobStore(Path.Combine(data, "queue.db"));
        store.RecoverInterrupted();
        ProfilePath.Text = Path.Combine(AppContext.BaseDirectory, "profiles", "capcut-9.6.vi.template.json");
        try
        {
            var file = Path.Combine(data, "preferences.json");
            if (File.Exists(file))
            {
                var prefs = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(file))!;
                ExePath.Text = prefs.GetValueOrDefault("exe", "");
                ProfilePath.Text = prefs.GetValueOrDefault("profile", ProfilePath.Text);
            }
            if (store.Settings() is { } settings)
            {
                OutputPath.Text = settings.OutputDirectory; Timeout.Text = settings.TimeoutMinutes.ToString();
                Resolution.SelectedIndex = Array.IndexOf(new[] { "720P", "1080P", "2K", "4K" }, settings.Resolution);
                Fps.SelectedIndex = Array.IndexOf(new[] { 24, 25, 30, 50, 60 }, settings.Fps);
            }
        }
        catch (Exception ex) { Log("Không đọc được thiết lập trước: " + ex.Message); }
        Reload();
        Log("Chỉ chạy trên desktop mở khóa. Dừng khẩn cấp: đưa chuột về góc trên trái.");
    }
    private ExportSettings Settings() => new(OutputPath.Text.Trim(),
        ((ComboBoxItem)Resolution.SelectedItem).Content.ToString()!,
        int.Parse(((ComboBoxItem)Fps.SelectedItem).Content.ToString()!), int.Parse(Timeout.Text));
    private void Reload() => JobsGrid.ItemsSource = store.Read();
    private void Error(Exception ex) => MessageBox.Show(this, ex.Message, "AutoRender", MessageBoxButton.OK, MessageBoxImage.Warning);
    private void Log(string text)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => Log(text)); return; }
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} {text}{Environment.NewLine}";
        LogBox.AppendText(line); LogBox.ScrollToEnd();
        try { File.AppendAllText(Path.Combine(data, "activity.log"), line); } catch (IOException) { }
    }
    private void Exe_Click(object sender, RoutedEventArgs e)
    { var d = new OpenFileDialog { Filter = "CapCut|CapCut.exe" }; if (d.ShowDialog() == true) ExePath.Text = d.FileName; }
    private void Profile_Click(object sender, RoutedEventArgs e)
    { var d = new OpenFileDialog { Filter = "UI profile JSON|*.json" }; if (d.ShowDialog() == true) ProfilePath.Text = d.FileName; }
    private void Folder_Click(object sender, RoutedEventArgs e)
    { var d = new OpenFolderDialog(); if (d.ShowDialog() == true) OutputPath.Text = d.FolderName; }
    private void Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = Settings(); settings.Validate();
            var dialog = new OpenFileDialog { Filter = "Danh sách UTF-8|*.txt" };
            if (dialog.ShowDialog() != true) return;
            var names = Playlist.Parse(File.ReadAllText(dialog.FileName, new UTF8Encoding(false, true)));
            if (store.Read().Count > 0 && MessageBox.Show(this, "Thay hàng đợi hiện tại? File video đã xuất được giữ nguyên.",
                    "Nhập danh sách", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            store.Replace(names, settings); Reload(); Status.Text = $"Đã nhập {names.Count} project";
            Log(Status.Text);
        }
        catch (Exception ex) { Error(ex); }
    }
    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        if (JobsGrid.SelectedItem is not Job job || job.State is not (JobState.Failed or JobState.NeedsReview)) return;
        if (File.Exists(job.Output)) { Error(new IOException("File đầu ra đã có. Kiểm tra và tự di chuyển file trước nếu muốn xuất lại.")); return; }
        if (MessageBox.Show(this, "Đã kiểm tra CapCut không còn xuất project này và đã trở về trang chủ?",
                "Cho phép chạy lại", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        store.Update(job.Id, JobState.Pending, "Được người dùng cho phép chạy lại"); Reload();
    }
    private void SetBusy(bool value)
    {
        busy = value; SettingsPanel.IsEnabled = QueueTools.IsEnabled = RunButton.IsEnabled = !value;
        StopButton.IsEnabled = value;
    }
    private async Task Work(Func<CancellationToken, Task> action)
    {
        if (busy) return;
        using var source = new CancellationTokenSource(); cancellation = source;
        SetBusy(true);
        try { await Task.Run(() => action(source.Token)); Status.Text = "Đã hoàn tất thao tác"; }
        catch (OperationCanceledException) { Status.Text = "Đã dừng · Kiểm tra CapCut trước khi tiếp tục"; Log(Status.Text); }
        catch (Exception ex) { Status.Text = "Đã dừng do lỗi · Xem nhật ký"; Log(ex.ToString()); Error(ex); }
        finally { cancellation = null; SetBusy(false); Reload(); if (closeAfterStop) Close(); }
    }
    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = Settings(); settings.Validate();
            if (!AspectConfirmed.IsChecked.GetValueOrDefault()) throw new InvalidOperationException("Xác nhận project đã có canvas 16:9 trước khi chạy.");
            if (store.Read().Count == 0) throw new InvalidOperationException("Nhập danh sách project trước.");
            if (settings != store.Settings()) throw new InvalidOperationException("Cấu hình khác hàng đợi đã lưu. Nhập lại danh sách để áp dụng cấu hình mới.");
            if (!store.Read().Any(j => j.State == JobState.Pending)) throw new InvalidOperationException("Không còn project chờ xuất.");
            if (store.Read().Any(j => j.State is JobState.Failed or JobState.NeedsReview)) throw new InvalidOperationException("Kiểm tra các hàng lỗi/cần kiểm tra trước khi chạy tiếp.");
            var profile = UiProfile.Load(ProfilePath.Text); profile.Validate(settings.Resolution, settings.Fps);
            File.WriteAllText(Path.Combine(data, "preferences.json"), JsonSerializer.Serialize(new Dictionary<string,string> {
                ["exe"] = ExePath.Text, ["profile"] = ProfilePath.Text }));
            var driver = new CapCutDriver(ExePath.Text, profile, Log);
            var runner = new QueueRunner(store, driver);
            runner.Changed += job => Dispatcher.Invoke(() => { Reload(); Status.Text = $"{job.Position}. {job.Name}: {job.State}"; Log(job.Message); });
            await Work(async token => { await driver.ConnectAsync(token); await runner.RunAsync(token); });
        }
        catch (Exception ex) { Error(ex); }
    }
    private async void Probe_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var d = new SaveFileDialog { Filter = "Khảo sát UI JSON|*.json", FileName = $"capcut-ui-{DateTime.Now:yyyyMMdd-HHmmss}.json" };
            if (d.ShowDialog() != true) return;
            var profile = new UiProfile(); var driver = new CapCutDriver(ExePath.Text, profile, Log);
            await Work(async token => { await driver.ConnectAsync(token); driver.Dump(d.FileName, token); Log("Đã ghi khảo sát: " + d.FileName); });
        }
        catch (Exception ex) { Error(ex); }
    }
    private void Stop_Click(object sender, RoutedEventArgs e)
    { cancellation?.Cancel(); Status.Text = "Đang yêu cầu dừng; không hủy render trong CapCut"; }
    private void OnClosing(object? sender, CancelEventArgs e)
    { if (busy) { e.Cancel = true; closeAfterStop = true; cancellation?.Cancel(); } }
}
