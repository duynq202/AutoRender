using AutoRender.Core;
using Xunit;
namespace AutoRender.Tests;
public sealed class ProfileTests
{
    [Fact] public void TemplateCannotStartAutomation()
    {
        var profile = new UiProfile();
        Assert.Throws<InvalidOperationException>(() => profile.Validate("1080P", 30));
    }
    [Fact] public void FlagAloneCannotBypassMissingSelectors()
    {
        var profile = new UiProfile { VerifiedOnThisPc = true };
        Assert.Throws<InvalidOperationException>(() => profile.Validate("1080P", 30));
    }
    [Fact] public void UnmappedExportOptionsAreRejected()
    {
        var profile = new UiProfile { VerifiedOnThisPc = true };
        foreach (var key in new[] { "search", "editorMarker", "openExport", "dialog", "fileName", "folder", "resolution", "fps", "format", "confirmExport", "success", "closeSuccess", "home" })
            profile.Controls[key] = new Selector(AutomationId: key);
        Assert.Throws<InvalidOperationException>(() => profile.Validate("1080P", 30));
        profile.ResolutionLabels["1080P"] = "1080P"; profile.FpsLabels["30"] = "30";
        profile.Validate("1080P", 30);
    }
}
