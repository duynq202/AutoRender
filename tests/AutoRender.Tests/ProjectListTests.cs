using AutoRender.Core;
using Xunit;
namespace AutoRender.Tests;
public sealed class ProjectListTests
{
    // Positions/text reproduced from the user's 2026-10-27 OCR log; boxes are representative.
    private static OcrText Name(double y = 812) => new("1027", 308, y, 35, 14);
    private static OcrText[] Row(double y = 812) => [Name(y), new("114.1K", 1048, y, 45, 14), new("00:03", 1150, y, 38, 14)];
    [Fact] public void ActualListLogProducesTargetOnSameRowNotProjectAbove()
    {
        var texts = new OcrText[] { new("DIJ ån", 240, 686, 60, 14), new("KB_05", 308, 766, 40, 14), new("KB_04", 308, 858, 40, 14) }.Concat(Row()).ToArray();
        var target = new ProjectLocator().Observe(texts, "1027", 1440, 960, 1);
        Assert.NotNull(target); Assert.Equal(ProjectLayout.List, target.Layout);
        Assert.Equal(819, target.Y); Assert.InRange(target.X, 344, 1047);
    }
    [Fact] public void FirstReportedCoordinateAlsoWorksInListMode()
    {
        var target = new ProjectLocator().Observe(Row(648), "1027", 1440, 960, 1);
        Assert.NotNull(target); Assert.Equal(655, target.Y); Assert.Equal(ProjectLayout.List, target.Layout);
    }
    [Fact] public void ListMetadataWorksWhenHeaderCannotBeRead()
        => Assert.Equal(ProjectLayout.List, new ProjectLocator().Observe(Row(), "1027", 1440, 960, 1)!.Layout);
    [Fact] public void OtherRowsDoNotAuthorizeName()
        => Assert.Null(new ProjectLocator().Observe([Name(), new("114.1K", 1048, 858, 45, 14), new("00:03", 1150, 858, 38, 14)], "1027", 1440, 960, 1));
    [Fact] public void DurationColumnIsRequiredWithoutSectionHeader()
        => Assert.Null(new ProjectLocator().Observe([Name(), new("114.1K", 1048, 812, 45, 14)], "1027", 1440, 960, 1));
    [Fact] public void MetadataBelowCaptionKeepsGridMode()
    {
        var target = new ProjectLocator().Observe([Name(), new("114.1K | 00:03", 308, 832, 110, 12)], "1027", 1440, 960, 1);
        Assert.NotNull(target); Assert.Equal(ProjectLayout.Grid, target.Layout); Assert.Equal(767, target.Y);
    }
}
