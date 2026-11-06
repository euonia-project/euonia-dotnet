// ReSharper disable All
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Extensions;

/// <summary>
/// 验证数值区间比较扩展方法在闭区间边界内外的返回值。
/// </summary>
public class ExtensionsCompareTest
{
    /// <summary>
    /// 验证值位于闭区间 [1, 5] 内（含两端）时 <c>IsNotInRange</c> 返回 <see langword="false"/>。
    /// </summary>
    [Fact]
    public void IsNotInRange_ReturnsFalseWhenWithinInclusiveRange()
    {
        Assert.False(5.IsNotInRange(1, 5));
        Assert.False(3.IsNotInRange(1, 5));
        Assert.False(1.IsNotInRange(1, 5));
    }

    /// <summary>
    /// 验证值低于下界或高于上界时 <c>IsNotInRange</c> 返回 <see langword="true"/>。
    /// </summary>
    [Fact]
    public void IsNotInRange_ReturnsTrueWhenOutsideRange()
    {
        Assert.True(0.IsNotInRange(1, 5));
        Assert.True(6.IsNotInRange(1, 5));
    }

    /// <summary>
    /// 验证 <c>IsBetween</c> 在闭区间 [1, 5] 内（含两端）返回 <see langword="true"/>，区间外返回 <see langword="false"/>。
    /// </summary>
    [Fact]
    public void IsBetween_ReturnsTrueWithinInclusiveRange()
    {
        Assert.True(5.IsBetween(1, 5));
        Assert.True(1.IsBetween(1, 5));
        Assert.False(6.IsBetween(1, 5));
    }
}