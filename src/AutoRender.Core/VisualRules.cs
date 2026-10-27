using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace AutoRender.Core;

public record OcrText(string Text, double X, double Y, double Width, double Height)
{
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
}
public static class VisualRules
{
    public static OcrText Merge(IReadOnlyList<OcrText> group)
    {
        var x = group.Min(w => w.X); var y = group.Min(w => w.Y);
        return new(string.Join(" ", group.Select(w => w.Text)), x, y,
            group.Max(w => w.X + w.Width) - x, group.Max(w => w.Y + w.Height) - y);
    }
    public static IReadOnlyList<OcrText> Rows(IEnumerable<OcrText> words)
    {
        var rows = new List<List<OcrText>>();
        foreach (var word in words.OrderBy(w => w.CenterY).ThenBy(w => w.X))
        {
            var row = rows.FirstOrDefault(r => Math.Abs(r.Average(t => t.CenterY) - word.CenterY) <= Math.Max(4, word.Height * .5));
            if (row == null) rows.Add([word]); else row.Add(word);
        }
        return rows.Select(r => Merge(r.OrderBy(w => w.X).ToArray())).ToArray();
    }
    public static string Key(string value)
    {
        var output = new StringBuilder();
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            var normalized = c is 'đ' or 'Đ' ? 'd' : char.ToLowerInvariant(c);
            if (char.IsLetterOrDigit(normalized)) output.Append(normalized);
        }
        return output.ToString();
    }
    public static void CheckNames(IEnumerable<string> names)
    {
        var seen = new HashSet<string>();
        foreach (var name in names)
            if (Key(name).Length == 0 || !seen.Add(Key(name)))
                throw new ArgumentException($"Tên không phân biệt được bằng OCR: {name}. Đổi tên project hoặc dùng danh sách không có tên trùng sau chuẩn hóa.");
    }
    public static OcrText Unique(IEnumerable<OcrText> texts, string expected, Func<OcrText, bool>? region = null)
    {
        var matches = texts.Where(t => Key(t.Text) == Key(expected) && (region?.Invoke(t) ?? true)).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"OCR cần đúng 1 kết quả '{expected}', nhận được {matches.Length}.");
        return matches[0];
    }
    public static bool IsSuccess(IEnumerable<OcrText> texts)
    {
        var lines = texts.Select(t => t.Text).ToArray();
        var key = Key(string.Join(" ", lines));
        if (!key.Contains("daluuvideo") || !lines.Any(t => Key(t) == "dong")) return false;
        var percentages = Regex.Matches(string.Join(" ", lines), @"(?<!\d)(\d{1,3})\s*%").Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        if (percentages.Any(p => p < 100)) return false;
        // The social preview in the supplied screenshot can still be generating at 66%.
        if (key.Contains("dangtao") && (percentages.Length == 0 || percentages.Any(p => p != 100))) return false;
        return true;
    }
    public static bool ValueMatches(string actual, string expected, bool path)
    {
        string Normalize(string s) => path ? s.Trim().Replace('\\', '/').ToUpperInvariant() : s.Trim();
        return Normalize(actual) == Normalize(expected);
    }
}
