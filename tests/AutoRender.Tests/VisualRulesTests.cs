using AutoRender.Core;
using Xunit;
namespace AutoRender.Tests;
public sealed class VisualRulesTests
{
    private static OcrText Line(string text, double x = 0, double y = 0) => new(text, x, y, text.Length * 7, 14);
    [Theory]
    [InlineData("Độ phân giải", "dophangiai")]
    [InlineData("30 fps", "30fps")]
    [InlineData("KB 01.3h", "kb013h")]
    public void VietnameseLabelsNormalized(string input, string expected) => Assert.Equal(expected, VisualRules.Key(input));
    [Fact] public void OcrNameCollisionsRejected()
        => Assert.Throws<ArgumentException>(() => VisualRules.CheckNames(["Dự án 1", "Du an 1"]));
    [Fact] public void PrefixProjectNamesAreNotAcceptedAsExactMatches()
        => Assert.Throws<InvalidOperationException>(() => VisualRules.Unique([Line("KB 01.3h")], "KB 01"));
    [Fact] public void SearchFieldAndProjectCardAreSeparatedByRegion()
    {
        var texts = new[] { Line("KB 02", 400, 40), Line("KB 02", 400, 200) };
        Assert.Throws<InvalidOperationException>(() => VisualRules.Unique(texts, "KB 02"));
        Assert.Equal(200, VisualRules.Unique(texts, "KB 02", t => t.Y > 100).Y);
    }
    [Fact] public void SuppliedSuccessScreenAt66PercentIsNotReady()
        => Assert.False(VisualRules.IsSuccess([Line("Đã lưu video vào máy tính để bàn hoặc máy tính"), Line("Đang tạo... 66 %"), Line("Đóng")]));
    [Fact] public void SuccessNeedsCloseControl()
        => Assert.False(VisualRules.IsSuccess([Line("Đã lưu video vào máy tính")]));
    [Fact] public void UnknownGenerationProgressIsNotReady()
        => Assert.False(VisualRules.IsSuccess([Line("Đã lưu video vào máy tính"), Line("Đang tạo..."), Line("Đóng")]));
    [Fact] public void CompletedSuccessCanProceed()
        => Assert.True(VisualRules.IsSuccess([Line("Đã lưu video vào máy tính"), Line("Đóng")]));
    [Fact] public void CompletedSocialPreviewCanProceed()
        => Assert.True(VisualRules.IsSuccess([Line("Đã lưu video vào máy tính"), Line("Đang tạo... 100 %"), Line("Đóng")]));
    [Fact] public void FileBeingCreatedIsNotSuccess()
        => Assert.False(VisualRules.IsSuccess([Line("Đang xuất 100%"), Line("Đóng")]));
    [Theory]
    [InlineData("E:/Videos/KB 02.mp4", "E:\\Videos\\KB 02.mp4", true)]
    [InlineData("E:/Videos/KB 02(2).mp4", "E:/Videos/KB 02.mp4", false)]
    [InlineData("E:/Wrong/KB 02.mp4", "E:/Videos/KB 02.mp4", false)]
    public void ReadbackValidatesEntireOutputPath(string actual, string expected, bool matches)
        => Assert.Equal(matches, VisualRules.ValueMatches(actual, expected, true));
    [Fact] public void FileNameReadbackIsExact()
        => Assert.False(VisualRules.ValueMatches("1027(2)", "1027", false));
    [Fact] public void ColumnWordsCanBeJoinedWithoutNeighborValue()
    {
        var words = new[] { new OcrText("Độ", 360, 280, 15, 12), new OcrText("phân", 380, 280, 25, 12), new OcrText("giải", 410, 280, 25, 12), new OcrText("720P", 464, 280, 35, 12) };
        var label = VisualRules.Rows(words.Where(w => w.X < 453));
        Assert.Single(label); Assert.Equal("dophangiai", VisualRules.Key(label[0].Text));
    }
    [Fact] public void VisualModeIsExplicitAndDoesNotEnableIncompleteUiaProfile()
    {
        var visual = new UiProfile { Engine = "Visual" }; visual.Validate("1080P", 30);
        var original = new UiProfile(); Assert.Throws<InvalidOperationException>(() => original.Validate("1080P", 30));
    }
}
