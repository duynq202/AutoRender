using System.Text.RegularExpressions;
namespace RenderBatch.Core;
public enum JobState { Pending, Running, Complete, Review }
public record Job(string Name, JobState State = JobState.Pending, string Detail = "")
{
    public string Label => State switch { JobState.Pending => "Chờ", JobState.Running => "Đang chạy", JobState.Complete => "Hoàn tất", _ => "Cần kiểm tra" };
}
public record Settings(string CapCutPath, string OutputFolder, string Resolution = "1080p", string Fps = "30",
    bool UseFolderPicker = true, int OpenSeconds = 180, int ExportMinutes = 720);
public sealed record Session(Settings Settings, List<Job> Jobs);
public static class Validation
{
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
        if (settings.OpenSeconds < 10 || settings.ExportMinutes is < 1 or > 10080) throw new ArgumentException("Thời gian chờ không hợp lệ.");
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
        ["home"] = Scene.Home, ["search-toggle"] = Scene.Home, ["search-field"] = Scene.Home,
        ["first-result"] = Scene.Home, ["editor-export"] = Scene.Editor, ["export-dialog"] = Scene.Export,
        ["name-field"] = Scene.Export, ["output-picker"] = Scene.Export, ["output-path"] = Scene.Export,
        ["resolution-menu"] = Scene.Export, ["fps-menu"] = Scene.Export, ["format-menu"] = Scene.Export,
        ["format-mp4"] = Scene.Export, ["confirm-export"] = Scene.Export,
        ["success"] = Scene.Success, ["close-success"] = Scene.Success
    };
    public static Dictionary<string, Scene> For(Settings s) => new(Fixed)
        { ["resolution-" + s.Resolution] = Scene.Export, ["fps-" + s.Fps] = Scene.Export };
    public static string Description(string role) => role switch
    {
        "home" => "Trang chủ: dấu hiệu vùng dự án", "search-toggle" => "Nút mở tìm kiếm", "search-field" => "Ô nhập tìm kiếm (đã mở)",
        "first-result" => "Nhãn Tên → hàng kết quả đầu tiên", "editor-export" => "Nút Xuất trong editor", "export-dialog" => "Dấu hiệu hộp cấu hình xuất",
        "name-field" => "Nhãn Tên → ô tên file", "output-picker" => "Nút mở hộp chọn thư mục", "output-path" => "Nhãn Xuất sang → ô đường dẫn",
        "resolution-menu" => "Nhãn độ phân giải → menu", "fps-menu" => "Nhãn FPS → menu", "format-menu" => "Nhãn định dạng → menu",
        "format-mp4" => "Giá trị MP4 đã chọn", "confirm-export" => "Nút bắt đầu xuất trong hộp xuất", "success" => "Thông báo Đã lưu video", "close-success" => "Nút đóng thông báo thành công",
        _ when role.StartsWith("resolution-") => "Giá trị độ phân giải: " + role[11..], _ when role.StartsWith("fps-") => "Giá trị FPS: " + role[4..], _ => role
    };
    public static IEnumerable<string> Required(Settings s) => For(s).Keys.Where(r => r != "output-picker" || s.UseFolderPicker);
}
