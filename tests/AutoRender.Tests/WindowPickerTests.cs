using AutoRender.Core;
using Xunit;
namespace AutoRender.Tests;
public sealed class WindowPickerTests
{
    private static WindowCandidate MainWindow(long id = 1) => new(id, 100, "QtWindow", "CapCut", true, false, false, 1400, 900);
    [Fact] public void UploadedTooltipCannotBeSelectedAsMainWindow()
    {
        var tooltip = new WindowCandidate(2, 100, "LVToolTipWindow_QMLTYPE_1748", "CapCut", true, false, false, 45, 17);
        Assert.Null(WindowPicker.Choose([tooltip], 2));
        Assert.Equal(1, WindowPicker.Choose([tooltip, MainWindow()], 2)!.Handle);
    }
    [Fact] public void LargeTooltipAndToolWindowAreExcluded()
    {
        var main = MainWindow();
        Assert.Equal(main, WindowPicker.Choose([main, main with { Handle = 2, ClassName = "Tooltip", Width = 2000 },
            main with { Handle = 3, ToolWindow = true, Width = 2200 }], 3));
    }
    [Fact] public void ForegroundApplicationWindowTakesPriority()
    {
        var editor = MainWindow(2) with { Width = 1000 };
        Assert.Equal(editor, WindowPicker.Choose([MainWindow(), editor], 2));
    }
    [Fact] public void HiddenWindowsAreIgnored() => Assert.Null(WindowPicker.Choose([MainWindow() with { Visible = false }], 0));
    [Fact] public void UnownedMainPreferredToInactiveOwnedDialog()
        => Assert.Equal(1, WindowPicker.Choose([MainWindow(), MainWindow(2) with { Owned = true, Width = 2000 }], 0)!.Handle);
    [Fact] public void AmbiguousEqualWindowsAreRejected()
        => Assert.Throws<InvalidOperationException>(() => WindowPicker.Choose([MainWindow(), MainWindow(2)], 0));
}
