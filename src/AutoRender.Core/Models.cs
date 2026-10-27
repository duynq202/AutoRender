namespace AutoRender.Core;

public enum JobState { Pending, Opening, Exporting, Completed, Failed, NeedsReview }
public record Job(long Id, int Position, string Name, string Output, JobState State, string Message)
{
    public string StateLabel => State switch
    {
        JobState.Pending => "Chờ xuất", JobState.Opening => "Đang mở",
        JobState.Exporting => "Đang xuất", JobState.Completed => "Hoàn tất",
        JobState.Failed => "Lỗi", JobState.NeedsReview => "Cần kiểm tra", _ => State.ToString()
    };
}
public record ExportSettings(string OutputDirectory, string Resolution = "1080P", int Fps = 30,
    int TimeoutMinutes = 720, int OpenTimeoutSeconds = 120, int PollMilliseconds = 2000)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(OutputDirectory) || !Path.IsPathFullyQualified(OutputDirectory))
            throw new ArgumentException("Chọn đường dẫn thư mục đầu ra đầy đủ.");
        if (Resolution is not ("720P" or "1080P" or "2K" or "4K"))
            throw new ArgumentException("Độ phân giải không hỗ trợ.");
        if (Fps is not (24 or 25 or 30 or 50 or 60)) throw new ArgumentException("FPS không hỗ trợ.");
        if (TimeoutMinutes < 1 || TimeoutMinutes > 10080 || OpenTimeoutSeconds < 1 || PollMilliseconds < 10)
            throw new ArgumentException("Thời gian chờ không hợp lệ.");
    }
}

public static class Playlist
{
    // Windows filename rules are enforced even when validation runs on Linux.
    private const string Invalid = "<>:\"/\\|?*";
    public static IReadOnlyList<string> Parse(string contents)
    {
        var names = contents.TrimStart('\uFEFF').Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        if (names.Length == 0) throw new ArgumentException("Danh sách không có tên project.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (!seen.Add(name)) throw new ArgumentException($"Tên project bị lặp: {name}");
            if (name.Length > 150 || name.EndsWith('.') || name.Any(c => c < 32 || Invalid.Contains(c)))
                throw new ArgumentException($"Tên không dùng được làm tên video Windows: {name}");
            var stem = name.Split('.')[0].ToUpperInvariant();
            if (stem is "CON" or "PRN" or "AUX" or "NUL" ||
                (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3] is >= '1' and <= '9'))
                throw new ArgumentException($"Tên video bị Windows dành riêng: {name}");
        }
        return names;
    }
}

public interface ICapCutDriver
{
    Task OpenProjectAsync(string name, ExportSettings settings, CancellationToken token);
    Task ConfigureAsync(string videoName, ExportSettings settings, CancellationToken token);
    Task BeginExportAsync(CancellationToken token);
    Task<bool> IsExportCompleteAsync(CancellationToken token);
    Task ReturnHomeAsync(CancellationToken token);
}
