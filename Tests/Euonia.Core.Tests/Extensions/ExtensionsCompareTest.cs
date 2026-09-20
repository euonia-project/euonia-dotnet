// ReSharper disable All
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Extensions;

public class ExtensionsCompareTest
{
    [Fact]
    public void IsNotInRange_ReturnsFalseWhenWithinInclusiveRange()
    {
        Assert.False(5.IsNotInRange(1, 5));
        Assert.False(3.IsNotInRange(1, 5));
        Assert.False(1.IsNotInRange(1, 5));
    }

    [Fact]
    public void IsNotInRange_ReturnsTrueWhenOutsideRange()
    {
        Assert.True(0.IsNotInRange(1, 5));
        Assert.True(6.IsNotInRange(1, 5));
    }

    [Fact]
    public void IsBetween_ReturnsTrueWithinInclusiveRange()
    {
        Assert.True(5.IsBetween(1, 5));
        Assert.True(1.IsBetween(1, 5));
        Assert.False(6.IsBetween(1, 5));
    }
}