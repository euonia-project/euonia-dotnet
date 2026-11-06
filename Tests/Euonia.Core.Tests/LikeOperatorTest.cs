// ReSharper disable All
using Xunit;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证 <see cref="LikeOperator"/> 的 SQL LIKE 风格通配符匹配，包括星号、问号、大小写与 <see langword="null"/> 处理。
/// </summary>
public class LikeOperatorTest
{
    /// <summary>
    /// 验证星号可匹配零个或多个字符，位于模式中间、末尾或开头时均能匹配。
    /// </summary>
    [Fact]
    public void Star_MatchesZeroOrMoreCharacters()
    {
        Assert.True(LikeOperator.LikeString("Hello World", "Hello*World"));
        Assert.True(LikeOperator.LikeString("Hello World", "Hello*"));
        Assert.True(LikeOperator.LikeString("Hello World", "*World"));
    }

    /// <summary>
    /// 验证问号恰好匹配一个字符，因此长度不符的内容不会匹配。
    /// </summary>
    [Fact]
    public void QuestionMark_MatchesSingleCharacter()
    {
        Assert.True(LikeOperator.LikeString("cat", "c?t"));
        Assert.False(LikeOperator.LikeString("cart", "c?t"));
    }

    /// <summary>
    /// 验证星号与问号组合时按顺序匹配，模式末尾存在多余字符则不匹配。
    /// </summary>
    [Fact]
    public void StarAndQuestionMark_Combine()
    {
        Assert.True(LikeOperator.LikeString("abcdef", "a*e?"));
        Assert.False(LikeOperator.LikeString("abcdef", "a*e?z"));
    }

    /// <summary>
    /// 验证默认忽略大小写，模式与内容大小写不同仍可匹配。
    /// </summary>
    [Fact]
    public void CaseInsensitiveByDefault()
    {
        Assert.True(LikeOperator.LikeString("Hello World", "HELLO*WORLD"));
    }

    /// <summary>
    /// 验证显式指定区分大小写时，大小写不同的内容不匹配。
    /// </summary>
    [Fact]
    public void CaseSensitive_WhenRequested()
    {
        Assert.False(LikeOperator.LikeString("Hello World", "hello*world", ignoreCase: false));
    }

    /// <summary>
    /// 验证位于两个字符之间的星号可匹配任意内容，包括空内容。
    /// </summary>
    [Fact]
    public void Star_BetweenCharacters_AllowsAnyContent()
    {
        Assert.True(LikeOperator.LikeString("abXYZcd", "ab*cd"));
        Assert.True(LikeOperator.LikeString("abcd", "ab*cd"));
    }

    /// <summary>
    /// 验证模式与内容不匹配时返回 <see langword="false"/>。
    /// </summary>
    [Fact]
    public void NoMatch_ReturnsFalse()
    {
        Assert.False(LikeOperator.LikeString("abc", "a*d"));
        Assert.False(LikeOperator.LikeString("abc", "x*"));
    }

    /// <summary>
    /// 验证内容与模式同时为 <see langword="null"/> 时视为匹配，仅一方为 <see langword="null"/> 时返回 <see langword="false"/>。
    /// </summary>
    [Fact]
    public void NullHandling()
    {
        Assert.True(LikeOperator.LikeString(null, null));
        Assert.False(LikeOperator.LikeString(null, "abc"));
        Assert.False(LikeOperator.LikeString("abc", null));
    }
}