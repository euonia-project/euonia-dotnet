// ReSharper disable All
namespace Nerosoft.Euonia.Core.Tests.Extensions;

public class ExtensionsTest
{
    [Fact]
    public void TestStringTruncateWithoutEllipsis()
    {
        var source = "TestStringTruncateWithoutEllipsis";
        var result = source.Truncate(15, false);
        Assert.Equal("TestStringTrunc", result);
    }

    [Fact]
    public void TestStringTruncateWithinEllipsis()
    {
        var source = "TestStringTruncateWithoutEllipsis";
        var result = source.Truncate(15, true);
        Assert.Equal("TestStringTrunc...", result);
    }

    [Fact]
    public void TestStringTruncateWithoutEllipsis_NullSource()
    {
        string source = null!;
        var result = source.Truncate(15, false);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TestStringTruncateWithinEllipsis_NullSource()
    {
        string source = null!;
        var result = source.Truncate(15, true);
        Assert.Equal(string.Empty, result);
    }
}
