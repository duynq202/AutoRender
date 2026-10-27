using System.Text.RegularExpressions;
namespace AutoRender.Core;

public enum ProjectLayout { Grid, List }
public record ProjectTarget(OcrText Caption, double X, double Y, ProjectLayout Layout = ProjectLayout.Grid);

/// <summary>Accept a project only with section or aligned size/duration evidence.</summary>
public sealed class ProjectLocator
{
    private bool reachedProjects;
    private double sectionLeft;
    public string LastReason { get; private set; } = "Chưa quét OCR.";
    private static readonly Regex Size = new(@"^\s*\d+(?:[.,]\d+)?\s*(?:[KMGT]B?|B)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Duration = new(@"(?<!\d)\d{1,3}[:：]\d{2}(?:[:：]\d{2})?(?!\d)", RegexOptions.CultureInvariant);
    public static bool HasCardMetadata(IReadOnlyList<OcrText> lines, OcrText caption, double scale)
    {
        var underneath = lines.Where(t => t.Y >= caption.Y + caption.Height - 2 * scale &&
            t.Y <= caption.Y + caption.Height + 36 * scale).ToArray();
        foreach (var start in underneath.Where(t => Math.Abs(t.X - caption.X) <= 24 * scale && Size.IsMatch(t.Text)))
        {
            var row = underneath.Where(t => Math.Abs(t.CenterY - start.CenterY) <= 6 * scale &&
                t.X >= start.X - 2 * scale && t.X < start.X + 145 * scale).OrderBy(t => t.X).ToArray();
            if (Duration.IsMatch(string.Join(" ", row.Select(t => t.Text)))) return true;
        }
        return false;
    }
    public static OcrText? ListSizeColumn(IReadOnlyList<OcrText> lines, OcrText caption, double scale)
    {
        var row = lines.Where(t => Math.Abs(t.CenterY - caption.CenterY) <= 8 * scale).ToArray();
        foreach (var size in row.Where(t => t.X > caption.X + caption.Width + 40 * scale && Size.IsMatch(t.Text)).OrderBy(t => t.X))
            if (row.Any(t => t.X >= size.X + size.Width - 2 * scale && Duration.IsMatch(t.Text))) return size;
        return null;
    }
    public ProjectTarget? Observe(IReadOnlyList<OcrText> lines, string name, double width, double height, double scale)
    {
        var names = lines.Where(t => VisualRules.Key(t.Text) == VisualRules.Key(name)).ToArray();
        // "DIJ ån" is the actual header OCR text in the user-provided log.
        var headers = lines.Where(t => VisualRules.Key(t.Text) is "duan" or "dijan" && t.X > width * .08).ToArray();
        if (headers.Length > 1) throw new InvalidOperationException("Không xác định duy nhất tiêu đề Dự án.");
        if (headers.Length == 1) { reachedProjects = true; sectionLeft = Math.Max(0, headers[0].X - 24 * scale); }
        if (names.Length == 0) { LastReason = $"OCR chưa đọc được tên '{name}' ở khung hình này."; return null; }
        var top = headers.Length == 1 ? headers[0].CenterY + 24 * scale : 70 * scale;
        var valid = new List<(OcrText Caption, bool Metadata, OcrText? ListSize)>();
        var reasons = new List<string>();
        foreach (var card in names)
        {
            var at = $"[{card.X:F0},{card.Y:F0}]";
            if (card.Width <= 0 || card.Height <= 0 || card.X < 0 || card.CenterX >= width || card.Y + card.Height >= height - 4 * scale)
            { reasons.Add($"{at}: dòng tên bị cắt hoặc nằm ngoài ảnh."); continue; }
            if (headers.Length == 1 && card.Y <= top)
            { reasons.Add($"{at}: tên nằm phía trên tiêu đề Dự án, không chọn thẻ AI."); continue; }
            var listSize = ListSizeColumn(lines, card, scale);
            var metadata = HasCardMetadata(lines, card, scale);
            var section = reachedProjects && card.X > sectionLeft && card.Y > top;
            if (!section && !metadata && listSize == null)
            { reasons.Add($"{at}: đã đọc được tên nhưng chưa xác nhận vùng project; thiếu tiêu đề Dự án, metadata dưới thumbnail hoặc cột dung lượng + thời lượng cùng hàng."); continue; }
            if (listSize == null && card.Y - 45 * scale <= (headers.Length == 1 ? top : 60 * scale))
            { reasons.Add($"{at}: thumbnail chưa hiển thị đủ, cần cuộn thêm."); continue; }
            valid.Add((card, metadata, listSize));
        }
        if (valid.Count > 1) throw new InvalidOperationException("OCR thấy nhiều project cùng tên. Không tự chọn.");
        if (valid.Count == 0) { LastReason = string.Join(" ", reasons); return null; }
        var chosen = valid[0];
        if (chosen.ListSize != null)
        {
            LastReason = $"Đã xác nhận '{name}' ở chế độ danh sách bằng cột dung lượng/thời lượng cùng hàng.";
            // Use blank space in the Name column, not the row above or editable title text.
            var x = (chosen.Caption.X + chosen.Caption.Width + chosen.ListSize.X) / 2;
            return new(chosen.Caption, x, chosen.Caption.CenterY, ProjectLayout.List);
        }
        LastReason = $"Đã xác nhận '{name}' bằng " + (chosen.Metadata ? "dòng dung lượng/thời lượng của card." : "tiêu đề vùng Dự án.");
        return new(chosen.Caption, chosen.Caption.CenterX, chosen.Caption.Y - 45 * scale);
    }
}
