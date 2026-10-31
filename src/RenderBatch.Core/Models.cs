using System.Text.RegularExpressions;
namespace RenderBatch.Core;
public enum JobState { Pending, Running, Complete, Review }
public record Job(string Name, JobState State = JobState.Pending, string Detail = "")
{
    public string Label => State switch { JobState.Pending => "Chờ", JobState.Running => "Đang chạy", JobState.Complete => "Hoàn tất", _ => "Cần kiểm tra" };
}
public record Settings(string CapCutPath, string OutputFolder, string Resolution = "1080p", string Fps = "30",
    bool UseFolderPicker = true, int OpenSeconds = 180, int ExportMinutes = 500, bool SimpleWorkflow = true, bool ShutdownWhenComplete = false);
public sealed record Session(Settings Settings, List<Job> Jobs);
public static class Validation
{
    public static bool CapCutVersion(string? version) =>
        Regex.IsMatch(version?.Trim() ?? "", @"^9\.(?:6|7)\.0(?:$|[.+-])");
    public static string[] Names(string text)
    {
        var names = text.TrimStart('\uFEFF').Split(['\r','\n'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        if (names.Length == 0) throw new ArgumentException("Danh sách project trống.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (!seen.Add(name)) throw new ArgumentException("Tên lặp: " + name);
            if (name.Length > 150 || name.EndsWith('.') || name.Any(c => c < 32 || "<>:\"/\\|?*".Contains(c)) ||
                Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)", RegexOptions.IgnoreCase))
                throw new ArgumentException("Tên không dùng được làm file Windows: " + name);
        }
        return names;
    }
    public static void Settings(Settings settings)
    {
        if (!Path.IsPathFullyQualified(settings.OutputFolder)) throw new ArgumentException("Chọn thư mục đầu ra đầy đủ.");
        if (settings.Resolution is not ("720p" or "1080p" or "4k") || settings.Fps is not ("30" or "60")) throw new ArgumentException("Cấu hình xuất không hợp lệ.");
        if (settings.OpenSeconds < 10 || settings.ExportMinutes is < 1 or > 500) throw new ArgumentException("Thời gian chờ không hợp lệ; chờ xuất phải từ 1 đến 500 phút.");
    }
    public static bool ExportTitleMatches(string title, string name)
    {
        var match = Regex.Match(title, @"^(?:Xuất|Export)\s*[-–—]\s*(.+)$", RegexOptions.IgnoreCase);
        return match.Success && string.Equals(match.Groups[1].Value.Trim(), name, StringComparison.OrdinalIgnoreCase);
    }
    public static bool OutputReady(string path)
    {
        try { using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None); return stream.Length > 0; }
        catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
    }
}
public static class Roles
{
    public static readonly Dictionary<string, Scene> Fixed = new()
    {
        ["home"] = Scene.Home, ["search-toggle"] = Scene.Home,
        ["first-result"] = Scene.Home, ["editor-export"] = Scene.Editor,
        ["confirm-export"] = Scene.Export, ["completion-actions"] = Scene.Success, ["completion-close"] = Scene.Success
    };
    public static Dictionary<string, Scene> For(Settings s)
    {
        var roles = new Dictionary<string, Scene>(Fixed);
        if (!s.SimpleWorkflow)
            foreach (var role in new[] { "export-dialog", "name-field", "output-picker", "output-path", "resolution-menu",
                "fps-menu", "format-menu", "format-mp4", "resolution-" + s.Resolution, "fps-" + s.Fps })
                if (role != "output-picker" || s.UseFolderPicker) roles[role] = Scene.Export;
        return roles;
    }
    public static string Description(string role) => role switch
    {
        "home" => "Trang chủ: dấu hiệu vùng dự án", "search-toggle" => "Nút mở tìm kiếm",
        "first-result" => "Nhãn Tên → hàng kết quả đầu tiên", "editor-export" => "Nút Xuất trong editor", "export-dialog" => "Dấu hiệu hộp cấu hình xuất",
        "name-field" => "Nhãn Tên → ô tên file", "output-picker" => "Nút mở hộp chọn thư mục", "output-path" => "Nhãn Xuất sang → ô đường dẫn",
        "resolution-menu" => "Nhãn độ phân giải → menu", "fps-menu" => "Nhãn FPS → menu", "format-menu" => "Nhãn định dạng → menu",
        "format-mp4" => "Giá trị MP4 đã chọn", "confirm-export" => "Nút bắt đầu xuất trong hộp xuất", "completion-actions" => "Hoàn tất: cặp nút Chia sẻ và Đóng", "completion-close" => "Hoàn tất: nút Đóng riêng",
        _ when role.StartsWith("resolution-") => "Giá trị độ phân giải: " + role[11..], _ when role.StartsWith("fps-") => "Giá trị FPS: " + role[4..], _ => role
    };
    public static IEnumerable<string> Required(Settings s) => For(s).Keys;
}
