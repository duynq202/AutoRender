namespace AutoRender.Core;

public record ProjectTarget(OcrText Caption, double X, double Y);

/// <summary>Locate a grid project strictly below the project section, never in the AI cards.</summary>
public sealed class ProjectLocator
{
    private bool reachedProjects;
    public ProjectTarget? Observe(IReadOnlyList<OcrText> lines, string name, double width, double height, double scale)
    {
        var headers = lines.Where(t => VisualRules.Key(t.Text) == "duan" && t.X > 180 * scale).ToArray();
        if (headers.Length > 1) throw new InvalidOperationException("Không xác định duy nhất tiêu đề Dự án.");
        if (headers.Length == 1) reachedProjects = true;
        if (!reachedProjects) return null;
        var top = headers.Length == 1 ? headers[0].CenterY + 24 * scale : 70 * scale;
        var candidates = lines.Where(t => t.X > 180 * scale && t.Y > top && t.Y + t.Height < height - 4 * scale &&
            VisualRules.Key(t.Text) == VisualRules.Key(name)).ToArray();
        if (candidates.Length > 1) throw new InvalidOperationException("OCR thấy nhiều project cùng tên. Không tự chọn.");
        if (candidates.Length == 0) return null;
        var card = candidates[0];
        var y = card.Y - 45 * scale;
        if (card.Width <= 0 || card.Height <= 0 || card.CenterX >= width || y <= top)
            return null; // Thumbnail is partly outside the visible project section.
        return new(card, card.CenterX, y);
    }
}
