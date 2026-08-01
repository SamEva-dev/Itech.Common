using Itech.Security.Contracts.Applications;

namespace Itech.Security.Contracts.Tests;

public sealed class ApplicationCodeTests
{
    [Fact]
    public void Constructor_NormalizesValue()
    {
        var code = new ApplicationCode("  DriveOS  ");

        Assert.Equal("driveos", code.Value);
    }

    [Fact]
    public void Constructor_RejectsBlankValue()
    {
        Assert.Throws<ArgumentException>(() => new ApplicationCode("  "));
    }
}
