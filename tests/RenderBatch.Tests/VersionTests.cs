using RenderBatch.Core;
using Xunit;
namespace RenderBatch.Tests;
public sealed class VersionTests
{
    [Theory]
    [InlineData("9.6.0",true)]
    [InlineData("9.6.0.89fd5606",true)]
    [InlineData("9.7.0",true)]
    [InlineData("9.7.0.123",true)]
    [InlineData("9.7.0+build",true)]
    [InlineData("9.7.01",false)]
    [InlineData("9.7.1",false)]
    [InlineData("9.70.0",false)]
    [InlineData("9.6.01",false)]
    [InlineData("10.0.0",false)]
    [InlineData("",false)]
    [InlineData(null,false)]
    public void OnlyDeclaredCapCutReleasesCanConnect(string? version,bool allowed) =>
        Assert.Equal(allowed,Validation.CapCutVersion(version));
}
