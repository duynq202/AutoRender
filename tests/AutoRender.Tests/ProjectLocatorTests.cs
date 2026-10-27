using AutoRender.Core;
using Xunit;
namespace AutoRender.Tests;
public sealed class ProjectLocatorTests
{
    private static OcrText Text(string value, double x, double y) => new(value, x, y, 80, 14);
    [Fact] public void AiCardAboveProjectSectionCannotBeChosen()
    {
        var locator = new ProjectLocator();
        Assert.Null(locator.Observe([Text("1027", 300, 350), Text("Dự án", 240, 700)], "1027", 1440, 960, 1));
    }
    [Fact] public void NoHeaderMeansNoProjectClickTarget()
        => Assert.Null(new ProjectLocator().Observe([Text("1027", 300, 700)], "1027", 1440, 960, 1));
    [Fact] public void VisibleGridCardIsLocatedWithoutSearchButtonCoordinates()
    {
        var target = new ProjectLocator().Observe([Text("Dự án", 240, 700), Text("1027", 250, 850)], "1027", 1440, 960, 1);
        Assert.NotNull(target); Assert.Equal(290, target.X); Assert.Equal(805, target.Y);
    }
    [Fact] public void HeaderNearTopIsAcceptedInCompactLayout()
        => Assert.NotNull(new ProjectLocator().Observe([Text("Dự án", 240, 100), Text("1027", 250, 300)], "1027", 1440, 960, 1));
    [Fact] public void MaximizedWindowKeepsFixedWidthSidebarBoundary()
        => Assert.NotNull(new ProjectLocator().Observe([Text("Dự án", 240, 300), Text("1027", 250, 500)], "1027", 1920, 1032, 1));
    [Fact] public void ContinuedScrollCanLocateCardAfterHeaderLeavesViewport()
    {
        var locator = new ProjectLocator();
        Assert.Null(locator.Observe([Text("Dự án", 240, 700)], "KB 04", 1440, 960, 1));
        Assert.NotNull(locator.Observe([Text("KB 04", 250, 400)], "KB 04", 1440, 960, 1));
    }
    [Fact] public void SidebarAndPartialThumbnailAreExcluded()
    {
        var locator = new ProjectLocator();
        Assert.Null(locator.Observe([Text("Dự án", 240, 200), Text("1027", 10, 400), Text("1027", 250, 240)], "1027", 1440, 960, 1));
    }
    [Fact] public void DuplicateVisibleProjectNamesFailClosed()
        => Assert.Throws<InvalidOperationException>(() => new ProjectLocator().Observe(
            [Text("Dự án", 240, 200), Text("1027", 250, 400), Text("1027", 450, 400)], "1027", 1440, 960, 1));
    [Fact] public void CroppedCaptionAtBottomIsNotUsed()
        => Assert.Null(new ProjectLocator().Observe([Text("Dự án", 240, 700), Text("1027", 250, 950)], "1027", 1440, 960, 1));
}
