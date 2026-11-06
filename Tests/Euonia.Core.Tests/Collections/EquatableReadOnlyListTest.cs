// ReSharper disable All
using System.Collections.Generic;
using System.Linq;
using Nerosoft.Euonia.Collections;
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Collections;

/// <summary>
/// 针对 <see cref="EquatableReadOnlyList{T}"/> 的相等性、哈希码与只读列表契约的测试。
/// </summary>
public class EquatableReadOnlyListTest
{
    /// <summary>
    /// 验证元素相同的两个列表相等：<c>==</c> 与 <c>Equals</c> 均为 <c>true</c>，<c>!=</c> 为 <c>false</c>。
    /// </summary>
    [Fact]
    public void Equals_ReturnsTrueForSameItems()
    {
        var first = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
        var second = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

        Assert.True(first == second);
        Assert.True(first.Equals(second));
        Assert.False(first != second);
    }

    /// <summary>
    /// 验证元素不同的两个列表不相等，<c>!=</c> 返回 <c>true</c>。
    /// </summary>
    [Fact]
    public void Equals_ReturnsFalseForDifferentItems()
    {
        var first = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
        var second = new EquatableReadOnlyList<int>(new[] { 1, 2, 4 });

        Assert.False(first == second);
        Assert.False(first.Equals(second));
        Assert.True(first != second);
    }

    /// <summary>
    /// 验证元素相同的两个列表具有相同的哈希码。
    /// </summary>
    [Fact]
    public void GetHashCode_IsEqualForEqualLists()
    {
        var first = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
        var second = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// 验证枚举按下标顺序返回全部元素，且计数与索引访问正确。
    /// </summary>
    [Fact]
    public void GetEnumerator_EnumeratesItems()
    {
        var list = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(new[] { 1, 2, 3 }, list.ToArray());
    }

    /// <summary>
    /// 验证默认值实例可以安全枚举且为空集合。
    /// </summary>
    [Fact]
    public void DefaultInstance_IsUsableAsReadOnlyList()
    {
        var list = default(EquatableReadOnlyList<int>);

        Assert.Empty(list);
    }

    /// <summary>
    /// 验证该类型可赋值给 <see cref="IReadOnlyList{T}"/>，且索引与计数访问正确。
    /// </summary>
    [Fact]
    public void ImplementsReadOnlyListInterface()
    {
        var list = new EquatableReadOnlyList<string>(new[] { "a", "b" });

        IReadOnlyList<string> readOnly = list;
        Assert.Equal("a", readOnly[0]);
        Assert.Equal(2, readOnly.Count);
    }
}