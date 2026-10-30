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
    Task<CompletionEvidence?> Success(CancellationToken token);
    Task Restart(string executable, CompletionEvidence completion, CancellationToken token);
}
public sealed class QueueRunner(IDesktop desktop, Action<Session> persist, Action<string> log, TimeProvider? clock = null, Action<string>? report = null)
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
                else log("Dùng tên file, thư mục, độ phân giải và FPS hiện tại của CapCut. File để kiểm tra bổ sung: " + output);
                if (File.Exists(output)) throw new IOException("File đầu ra vừa xuất hiện. Không ghi đè.");
                if (await desktop.Success(token) is not null) throw new InvalidOperationException("Bảng hoàn tất cũ đang hiện trước khi bắt đầu xuất.");
                await desktop.Click("confirm-export", token);
                Update(JobState.Running, $"Đang xuất; tối đa {settings.ExportMinutes} phút, kiểm tra mỗi 5 giây");
                var time = clock ?? TimeProvider.System; var started = time.GetTimestamp();
                var exportLimit = TimeSpan.FromMinutes(settings.ExportMinutes);
                var scanInterval = TimeSpan.FromSeconds(5);
                var nextScan = scanInterval;
                CompletionEvidence? verified = null;
                var seenLogged = false;
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var elapsed = time.GetElapsedTime(started);
                    if (elapsed >= exportLimit)
                        throw new TimeoutException("Hết thời gian chờ xác nhận ảnh nút Đóng; CapCut được giữ mở.");
                    // Quiet five-second scans: no polling entries are written to the UI log.
                    // The last scan is allowed at the deadline; the delay never exceeds it.
                    var due = nextScan < exportLimit ? nextScan : exportLimit;
                    if (due > elapsed) await desktop.Pause(due - elapsed, token);
                    var first = await desktop.Success(token);
                    if (first is not null)
                    {
                        if (!seenLogged) { log("Đã thấy nút Đóng; xác nhận lại sau 1 giây."); seenLogged = true; }
                        report?.Invoke("Đã thấy nút Đóng; đang xác nhận bằng ảnh chụp mới");
                        await desktop.Pause(TimeSpan.FromSeconds(1), token);
                        var second = await desktop.Success(token);
                        if (first.Matches(second))
                        {
                            token.ThrowIfCancellationRequested();
                            verified = second;
                            var fileReady = Validation.OutputReady(output);
                            var detail = fileReady ? "Đã xác nhận nút Đóng hai lần; MP4 kiểm tra được"
                                : "Hoàn tất theo ảnh; cần kiểm tra lại file MP4";
                            Update(JobState.Complete, detail);
                            if (!fileReady) log("Cảnh báo: chưa kiểm tra được MP4 đúng tên hoặc file còn bị khóa: " + output + ". Tiếp tục đóng/mở lại CapCut theo ảnh hoàn tất.");
                            break;
                        }
                    }
                    report?.Invoke("Chưa xác nhận ổn định ảnh nút Đóng; quét lại ở mốc 5 giây tiếp theo");
                    // Keep scans on five-second boundaries even when image recognition takes time.
                    nextScan = TimeSpan.FromSeconds((Math.Floor(time.GetElapsedTime(started).TotalSeconds / scanInterval.TotalSeconds) + 1) * scanInterval.TotalSeconds);
                }
                report?.Invoke("Video đã xuất xong; đang đóng và mở lại CapCut");
                log("Đã xác nhận hoàn tất. Đóng ứng dụng CapCut và mở lại...");
                await desktop.Restart(settings.CapCutPath, verified!, token);
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
