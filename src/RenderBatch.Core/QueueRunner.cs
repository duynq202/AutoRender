namespace RenderBatch.Core;
public interface IDesktop
{
    Task Wait(string role, CancellationToken token);
    Task Click(string role, CancellationToken token);
    Task Search(string name, CancellationToken token);
    Task OpenExportDialog(CancellationToken token);
    Task Pause(TimeSpan duration, CancellationToken token);
    Task<string> ExportTitle(CancellationToken token);
    Task Fill(string role, string value, CancellationToken token);
    Task<string> Read(string role, CancellationToken token);
    Task ChooseFolder(string folder, CancellationToken token);
    Task Select(string menu, string value, CancellationToken token);
    Task<bool> Success(CancellationToken token);
    Task Restart(string executable, CancellationToken token);
}
public sealed class QueueRunner(IDesktop desktop, Action<Session> persist, Action<string> log, TimeProvider? clock = null)
{
    public async Task Run(Session session, CancellationToken token)
    {
        var settings = session.Settings; Validation.Settings(settings);
        if (session.Jobs.Any(j => j.State is JobState.Running or JobState.Review)) throw new InvalidOperationException("Kiểm tra các project bị gián đoạn và nhập lại danh sách trước khi chạy.");
        Directory.CreateDirectory(settings.OutputFolder);
        for (var i = 0; i < session.Jobs.Count; i++)
        {
            if (session.Jobs[i].State == JobState.Complete) continue;
            token.ThrowIfCancellationRequested(); var name = session.Jobs[i].Name;
            void Update(JobState state, string message) { session.Jobs[i] = new(name, state, message); persist(session); log($"{i+1}/{session.Jobs.Count} · {name}: {message}"); }
            var output = Path.Combine(settings.OutputFolder, name + ".mp4");
            try
            {
                if (File.Exists(output) || Directory.Exists(output)) throw new IOException("Đầu ra đã tồn tại, không ghi đè: " + output);
                Update(JobState.Running, "Đang tìm bằng ảnh mẫu");
                await desktop.Wait("home", token); await desktop.Search(name, token);
                await desktop.Click("first-result", token);
                Update(JobState.Running, "Đang mở bảng xuất; thử nút Xuất mỗi 5 giây, tối đa 120 giây");
                await desktop.OpenExportDialog(token);
                await desktop.Wait(settings.SimpleWorkflow ? "confirm-export" : "export-dialog", token);
                if (!Validation.ExportTitleMatches(await desktop.ExportTitle(token), name))
                    throw new InvalidOperationException("Tên project trong tiêu đề hộp xuất không khớp. Đã dừng trước khi export.");
                if (!settings.SimpleWorkflow)
                {
                    await desktop.Fill("name-field", name, token);
                    if (settings.UseFolderPicker) await desktop.ChooseFolder(settings.OutputFolder, token);
                    else await desktop.Fill("output-path", output, token);
                    await desktop.Fill("name-field", name, token);
                    var actualPath = await desktop.Read("output-path", token);
                    if (!string.Equals(actualPath.Trim().Replace('/', '\\'), output.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Đường dẫn file xuất đọc lại không đúng. Không nhấn Xuất.");
                    await desktop.Select("resolution-menu", "resolution-" + settings.Resolution, token);
                    await desktop.Select("fps-menu", "fps-" + settings.Fps, token);
                    await desktop.Select("format-menu", "format-mp4", token);
                }
                else log("Dùng tên file, thư mục, độ phân giải và FPS hiện tại của CapCut. Chờ file: " + output);
                if (File.Exists(output)) throw new IOException("File đầu ra vừa xuất hiện. Không ghi đè.");
                if (await desktop.Success(token)) throw new InvalidOperationException("Ảnh thành công cũ đang hiện trước khi bắt đầu xuất.");
                await desktop.Click("confirm-export", token);
                Update(JobState.Running, $"Đang xuất; tối đa {settings.ExportMinutes} phút, kiểm tra mỗi phút");
                var time = clock ?? TimeProvider.System; var started = time.GetTimestamp();
                var exportLimit = TimeSpan.FromMinutes(settings.ExportMinutes);
                var nextScan = TimeSpan.FromMinutes(1);
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var elapsed = time.GetElapsedTime(started);
                    if (elapsed >= exportLimit)
                        throw new TimeoutException("Hết thời gian chờ ảnh thành công và file video: " + output);
                    // Quiet minute scans: no polling entries are written to the UI log.
                    // The last scan is allowed at the deadline; the delay never exceeds it.
                    var due = nextScan < exportLimit ? nextScan : exportLimit;
                    if (due > elapsed) await desktop.Pause(due - elapsed, token);
                    var success = await desktop.Success(token);
                    if (success && Validation.OutputReady(output)) break;
                    // Keep scans on minute boundaries even when image recognition takes time.
                    nextScan = TimeSpan.FromMinutes(Math.Floor(time.GetElapsedTime(started).TotalMinutes) + 1);
                }
                Update(JobState.Complete, "CapCut báo thành công; file có dữ liệu và đã đóng");
                log("Đã xuất xong. Đóng và mở lại CapCut...");
                await desktop.Restart(settings.CapCutPath, token);
                await desktop.Wait("home", token);
            }
            catch (Exception ex)
            {
                if (session.Jobs[i].State != JobState.Complete) Update(JobState.Review, ex is OperationCanceledException ? "Đã dừng. Kiểm tra render trong CapCut." : ex.Message);
                throw;
            }
        }
    }
}
