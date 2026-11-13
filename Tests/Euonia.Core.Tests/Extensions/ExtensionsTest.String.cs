// ReSharper disable All
namespace Nerosoft.Euonia.Core.Tests.Extensions;

/// <summary>
/// 验证字符串截断扩展方法在关闭或开启省略号以及源字符串为 <see langword="null"/> 时的行为。
/// </summary>
public class ExtensionsTest
{
    /// <summary>
    /// 验证不追加省略号时按长度 15 截断源字符串，结果为前 15 个字符。
    /// </summary>
    [Fact]
    public void TestStringTruncateWithoutEllipsis()
    {
        var source = "TestStringTruncateWithoutEllipsis";
        var result = source.Truncate(15, false);
        Assert.Equal("TestStringTrunc", result);
    }

    /// <summary>
    /// 验证启用省略号时结果由前 12 个字符加三个点组成，总长度仍为 15。
    /// </summary>
    [Fact]
    public void TestStringTruncateWithinEllipsis()
    {
        var source = "TestStringTruncateWithoutEllipsis";
        var result = source.Truncate(15, true);
        Assert.Equal("TestStringTrunc...", result);
    }

    /// <summary>
    /// 验证源字符串为 <see langword="null"/> 且不追加省略号时返回空字符串。
    /// </summary>
    [Fact]
    public void TestStringTruncateWithoutEllipsis_NullSource()
    {
        string source = null!;
        var result = source.Truncate(15, false);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// 验证源字符串为 <see langword="null"/> 且启用省略号时仍返回空字符串。
    /// </summary>
    [Fact]
    public void TestStringTruncateWithinEllipsis_NullSource()
    {
        string source = null!;
        var result = source.Truncate(15, true);
        Assert.Equal(string.Empty, result);
    }
}
