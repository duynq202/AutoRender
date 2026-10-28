namespace RenderBatch.Core;
public interface IDesktop
{
    Task Wait(string role, CancellationToken token);
    Task Click(string role, CancellationToken token);
    Task Search(string name, CancellationToken token);
    Task<string> ExportTitle(CancellationToken token);
    Task Fill(string role, string value, CancellationToken token);
    Task<string> Read(string role, CancellationToken token);
    Task ChooseFolder(string folder, CancellationToken token);
    Task Select(string menu, string value, CancellationToken token);
    Task<bool> Success(CancellationToken token);
    Task Home(CancellationToken token);
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
                await desktop.Click("first-result", token); await desktop.Wait("editor-export", token);
                await desktop.Click("editor-export", token); await desktop.Wait("export-dialog", token);
                if (!Validation.ExportTitleMatches(await desktop.ExportTitle(token), name))
                    throw new InvalidOperationException("Tên project trong tiêu đề hộp xuất không khớp. Đã dừng trước khi export.");
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
                if (File.Exists(output)) throw new IOException("File đầu ra vừa xuất hiện. Không ghi đè.");
                if (await desktop.Success(token)) throw new InvalidOperationException("Ảnh thành công cũ đang hiện trước khi bắt đầu xuất.");
                await desktop.Click("confirm-export", token); log("Đang xuất: " + name);
                var time = clock ?? TimeProvider.System; var started = time.GetTimestamp();
                while (!(await desktop.Success(token) && Validation.OutputReady(output)))
                {
                    token.ThrowIfCancellationRequested();
                    if (time.GetElapsedTime(started) > TimeSpan.FromMinutes(settings.ExportMinutes)) throw new TimeoutException("Hết thời gian chờ export.");
                    await Task.Delay(5000, token);
                }
                Update(JobState.Complete, "CapCut báo thành công; file có dữ liệu và đã đóng");
                await desktop.Click("close-success", token); await desktop.Home(token);
            }
            catch (Exception ex)
            {
                if (session.Jobs[i].State != JobState.Complete) Update(JobState.Review, ex is OperationCanceledException ? "Đã dừng. Kiểm tra render trong CapCut." : ex.Message);
                throw;
            }
        }
    }
}
