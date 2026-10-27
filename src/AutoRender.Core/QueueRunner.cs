namespace AutoRender.Core;

public sealed class QueueRunner(JobStore store, ICapCutDriver driver, TimeProvider? clock = null)
{
    public event Action<Job>? Changed;
    private void State(Job job, JobState state, string text)
    {
        store.Update(job.Id, state, text);
        Changed?.Invoke(job with { State = state, Message = text });
    }
    public static bool OutputReady(string path)
    {
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return file.Length > 0;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
    public async Task RunAsync(CancellationToken token)
    {
        var settings = store.Settings() ?? throw new InvalidOperationException("Chưa có cấu hình hàng đợi.");
        settings.Validate();
        Directory.CreateDirectory(settings.OutputDirectory);
        var jobs = store.Read();
        if (jobs.Any(j => j.State is JobState.Failed or JobState.NeedsReview or JobState.Opening or JobState.Exporting))
            throw new InvalidOperationException("Xử lý các hàng lỗi/cần kiểm tra trước khi tiếp tục.");
        foreach (var job in jobs.Where(j => j.State == JobState.Pending))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (File.Exists(job.Output) || Directory.Exists(job.Output))
                    throw new IOException($"Đầu ra đã tồn tại, không ghi đè: {job.Output}");
                State(job, JobState.Opening, "Đang tìm và mở project");
                await driver.OpenProjectAsync(job.Name, settings, token);
                await driver.ConfigureAsync(job.Name, settings, token);
                // Configuration can take time: protect existing files again before clicking Export.
                if (File.Exists(job.Output)) throw new IOException("File đầu ra vừa xuất hiện; dừng để tránh ghi đè.");
                State(job, JobState.Exporting, "Đang xuất; chờ CapCut báo hoàn tất và file được đóng");
                await driver.BeginExportAsync(token);
                var time = clock ?? TimeProvider.System;
                var started = time.GetTimestamp();
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    if (await driver.IsExportCompleteAsync(token) && OutputReady(job.Output)) break;
                    if (time.GetElapsedTime(started) > TimeSpan.FromMinutes(settings.TimeoutMinutes))
                        throw new TimeoutException("Hết thời gian chờ. Không tự chuyển sang project tiếp theo.");
                    await Task.Delay(settings.PollMilliseconds, token);
                }
                State(job, JobState.Completed, "CapCut báo hoàn tất; file đầu ra có dữ liệu và đã đóng");
                try { await driver.ReturnHomeAsync(token); }
                catch (Exception ex)
                {
                    State(job, JobState.Completed, $"Video đã xuất; chưa về được trang chủ: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (store.Read().Single(j => j.Id == job.Id).State != JobState.Completed)
                    State(job, ex is OperationCanceledException ? JobState.NeedsReview : JobState.Failed,
                        ex is OperationCanceledException ? "Đã dừng. Tác vụ xuất trong CapCut có thể vẫn chạy." : ex.Message);
                throw;
            }
        }
    }
}
