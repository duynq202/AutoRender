using OpenCvSharp;
namespace RenderBatch.Core;

public enum Scene { Home, Editor, Export, Success }
public record Box(int X, int Y, int Width, int Height);
public record Asset(string Role, Scene Scene, string File, int WindowWidth, int WindowHeight, uint Dpi,
    Box Region, double ClickX, double ClickY, int Padding = 64);
public record TemplateHit(double Score, double X, double Y);
public sealed class TemplateProfile
{
    public Dictionary<string, Asset> Assets { get; set; } = new();
    public double Threshold { get; set; } = .97;
    public Asset Require(string role) => Assets.TryGetValue(role, out var asset) ? asset
        : throw new InvalidOperationException($"Chưa học ảnh: {role}");
    public void Validate(IEnumerable<string> roles)
    {
        if (Threshold is < .9 or > 1) throw new InvalidOperationException("Ngưỡng tìm ảnh phải nằm trong 0.90–1.00.");
        foreach (var role in roles) Require(role);
    }
}
public static class TemplateMatcher
{
    public static TemplateHit? Find(byte[] frameBytes, byte[] templateBytes, Asset asset, uint dpi, double threshold, bool tight = false, bool allowResize = false)
    {
        using var frame = Cv2.ImDecode(frameBytes, ImreadModes.Grayscale);
        using var template = Cv2.ImDecode(templateBytes, ImreadModes.Grayscale);
        if (frame.Empty() || template.Empty()) throw new InvalidDataException("Ảnh không đọc được.");
        if ((!allowResize && (frame.Width != asset.WindowWidth || frame.Height != asset.WindowHeight)) || dpi != asset.Dpi)
            throw new InvalidOperationException($"Cửa sổ/DPI khác lúc học ảnh '{asset.Role}'. Giữ kích thước cũ hoặc học lại ảnh.");
        Cv2.MeanStdDev(template, out _, out var deviation);
        if (template.Width < 8 || template.Height < 8 || deviation.Val0 < 5)
            throw new InvalidDataException("Ảnh mẫu quá nhỏ hoặc chỉ có nền phẳng. Chọn biểu tượng/chữ rõ nét.");
        var padding = tight ? (allowResize ? 48 : 8) : asset.Padding;
        var rx = allowResize ? (int)Math.Round((double)asset.Region.X * frame.Width / asset.WindowWidth) : asset.Region.X;
        var ry = allowResize ? (int)Math.Round((double)asset.Region.Y * frame.Height / asset.WindowHeight) : asset.Region.Y;
        var region = new Rect(Math.Max(0, rx - padding), Math.Max(0, ry - padding), 1, 1);
        region.Width = Math.Min(frame.Width, rx + asset.Region.Width + padding) - region.X;
        region.Height = Math.Min(frame.Height, ry + asset.Region.Height + padding) - region.Y;
        if (region.Width < template.Width || region.Height < template.Height) return null;
        using var crop = new Mat(frame, region); using var scores = new Mat();
        Cv2.MatchTemplate(crop, template, scores, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(scores, out _, out var best, out _, out var point);
        if (double.IsNaN(best) || best < threshold) return null;
        var overlap = new Rect(Math.Max(0, point.X - template.Width + 1), Math.Max(0, point.Y - template.Height + 1), 1, 1);
        overlap.Width = Math.Min(scores.Width, point.X + template.Width) - overlap.X;
        overlap.Height = Math.Min(scores.Height, point.Y + template.Height) - overlap.Y;
        using (var duplicateArea = new Mat(scores, overlap)) duplicateArea.SetTo(-1);
        Cv2.MinMaxLoc(scores, out double _, out double second);
        if (second >= threshold && best - second < .025)
            throw new InvalidOperationException($"Ảnh '{asset.Role}' khớp nhiều nơi. Học ảnh có thêm ngữ cảnh hoặc thu hẹp vùng tìm.");
        var x = region.X + point.X + asset.ClickX;
        var y = region.Y + point.Y + asset.ClickY;
        if (x < 0 || y < 0 || x >= frame.Width || y >= frame.Height)
            throw new InvalidDataException("Điểm bấm đã học nằm ngoài cửa sổ.");
        return new(best, x, y);
    }
}
