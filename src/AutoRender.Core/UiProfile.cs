using System.IO;
using System.Text.Json;
namespace AutoRender.Core;

public record Selector(string? Name = null, string? AutomationId = null, string? ControlType = null)
{
    public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(AutomationId);
}
public sealed class UiProfile
{
    public string Version { get; set; } = "9.6.0";
    public bool VerifiedOnThisPc { get; set; }
    public string ProjectControlType { get; set; } = "Text";
    public bool FileNameIncludesExtension { get; set; }
    public Dictionary<string, Selector> Controls { get; set; } = new();
    public Dictionary<string, string> ResolutionLabels { get; set; } = new();
    public Dictionary<string, string> FpsLabels { get; set; } = new();
    public string Mp4Label { get; set; } = "MP4";
    public void Validate(string resolution, int fps)
    {
        if (!VerifiedOnThisPc) throw new InvalidOperationException("Profile chưa được kiểm chứng trên PC này. Khảo sát giao diện và hiệu chỉnh profile trước.");
        foreach (var key in new[] { "search", "editorMarker", "openExport", "dialog", "fileName", "folder", "resolution", "fps", "format", "confirmExport", "success", "closeSuccess", "home" })
            if (!Controls.TryGetValue(key, out var value) || value.IsEmpty)
                throw new InvalidOperationException($"Profile thiếu selector: {key}");
        if (!ResolutionLabels.ContainsKey(resolution) || !FpsLabels.ContainsKey(fps.ToString()))
            throw new InvalidOperationException("Profile chưa có nhãn UI cho độ phân giải hoặc FPS đã chọn.");
        if (string.IsNullOrWhiteSpace(ProjectControlType) || string.IsNullOrWhiteSpace(Mp4Label))
            throw new InvalidOperationException("Profile thiếu loại phần tử project hoặc nhãn MP4.");
    }
    public static UiProfile Load(string path) => JsonSerializer.Deserialize<UiProfile>(File.ReadAllText(path))
        ?? throw new InvalidDataException("Profile không hợp lệ.");
}
