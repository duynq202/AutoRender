using AutoRender.Core;
using Xunit;
namespace AutoRender.Tests;
public sealed class ProjectMetadataTests
{
    private static OcrText Name => new("1027", 308, 648, 40, 14);
    private static OcrText Meta(string text, double x = 308, double y = 668) => new(text, x, y, 110, 12);
    [Fact] public void ReportedNameCoordinateWithCardMetadataWorksWithoutHeader()
    {
        var target = new ProjectLocator().Observe([Name, Meta("114.1K | 00:03")], "1027", 1440, 960, 1);
        Assert.NotNull(target); Assert.Equal(328, target.X); Assert.Equal(603, target.Y);
    }
    [Fact] public void NameAloneReportsMissingSectionEvidence()
    {
        var locator = new ProjectLocator(); Assert.Null(locator.Observe([Name], "1027", 1440, 960, 1));
        Assert.Contains("đã đọc được tên", locator.LastReason); Assert.Contains("[308,648]", locator.LastReason);
    }
    [Theory]
    [InlineData("152.4M | 02:45:44")][InlineData("9.5G 02:42:33")][InlineData("114.1KB|00:03")]
    public void CommonSizeDurationRowsAccepted(string metadata)
        => Assert.True(ProjectLocator.HasCardMetadata([Name, Meta(metadata)], Name, 1));
    [Fact] public void SeparateOcrFragmentsOnMetadataRowAreJoined()
        => Assert.True(ProjectLocator.HasCardMetadata([Name, Meta("114.1K"), Meta("00:03", 365)], Name, 1));
    [Theory]
    [InlineData("00:03")][InlineData("114.1K")][InlineData("Tạo video bằng AI")]
    public void DurationOrSizeAloneDoesNotProveProject(string text)
        => Assert.False(ProjectLocator.HasCardMetadata([Name, Meta(text)], Name, 1));
    [Fact] public void OtherCardsMetadataCannotAuthorizeName()
        => Assert.Null(new ProjectLocator().Observe([Name, Meta("114.1K | 00:03", 460)], "1027", 1440, 960, 1));
    [Fact] public void MetadataFarBelowCannotAuthorizeName()
        => Assert.False(ProjectLocator.HasCardMetadata([Name, Meta("114.1K | 00:03", y: 780)], Name, 1));
    [Fact] public void DynamicSectionLeftAvoidsFixedDpiBoundaryRejectingReadableName()
        => Assert.NotNull(new ProjectLocator().Observe([new("Dự án", 308, 500, 60, 16), Name], "1027", 1440, 960, 2));
    [Fact] public void RecognizedHeaderStillExcludesAiCardAboveIt()
        => Assert.Null(new ProjectLocator().Observe([Name, Meta("114.1K | 00:03"), new("Dự án", 300, 780, 60, 16)], "1027", 1440, 960, 1));
    [Fact] public void DuplicateProvenCardsAreNotAutomaticallyChosen()
        => Assert.Throws<InvalidOperationException>(() => new ProjectLocator().Observe(
            [Name, Meta("114.1K | 00:03"), new("1027", 500, 648, 40, 14), Meta("5M | 00:30", 500)], "1027", 1440, 960, 1));
}
