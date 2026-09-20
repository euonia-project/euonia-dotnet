// ReSharper disable All
using Xunit;

namespace Nerosoft.Euonia.Core.Tests;

public class LikeOperatorTest
{
    [Fact]
    public void Star_MatchesZeroOrMoreCharacters()
    {
        Assert.True(LikeOperator.LikeString("Hello World", "Hello*World"));
        Assert.True(LikeOperator.LikeString("Hello World", "Hello*"));
        Assert.True(LikeOperator.LikeString("Hello World", "*World"));
    }

    [Fact]
    public void QuestionMark_MatchesSingleCharacter()
    {
        Assert.True(LikeOperator.LikeString("cat", "c?t"));
        Assert.False(LikeOperator.LikeString("cart", "c?t"));
    }

    [Fact]
    public void StarAndQuestionMark_Combine()
    {
        Assert.True(LikeOperator.LikeString("abcdef", "a*e?"));
        Assert.False(LikeOperator.LikeString("abcdef", "a*e?z"));
    }

    [Fact]
    public void CaseInsensitiveByDefault()
    {
        Assert.True(LikeOperator.LikeString("Hello World", "HELLO*WORLD"));
    }

    [Fact]
    public void CaseSensitive_WhenRequested()
    {
        Assert.False(LikeOperator.LikeString("Hello World", "hello*world", ignoreCase: false));
    }

    [Fact]
    public void Star_BetweenCharacters_AllowsAnyContent()
    {
        Assert.True(LikeOperator.LikeString("abXYZcd", "ab*cd"));
        Assert.True(LikeOperator.LikeString("abcd", "ab*cd"));
    }

    [Fact]
    public void NoMatch_ReturnsFalse()
    {
        Assert.False(LikeOperator.LikeString("abc", "a*d"));
        Assert.False(LikeOperator.LikeString("abc", "x*"));
    }

    [Fact]
    public void NullHandling()
    {
        Assert.True(LikeOperator.LikeString(null, null));
        Assert.False(LikeOperator.LikeString(null, "abc"));
        Assert.False(LikeOperator.LikeString("abc", null));
    }
}