// ReSharper disable All
using System.Collections.Generic;
using System.Linq;
using Nerosoft.Euonia.Collections;
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Collections;

public class EquatableReadOnlyListTest
{
    [Fact]
    public void Equals_ReturnsTrueForSameItems()
    {
        var first = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
        var second = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

        Assert.True(first == second);
        Assert.True(first.Equals(second));
        Assert.False(first != second);
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentItems()
    {
        var first = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
        var second = new EquatableReadOnlyList<int>(new[] { 1, 2, 4 });

        Assert.False(first == second);
        Assert.False(first.Equals(second));
        Assert.True(first != second);
    }

    [Fact]
    public void GetHashCode_IsEqualForEqualLists()
    {
        var first = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
        var second = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void GetEnumerator_EnumeratesItems()
    {
        var list = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(new[] { 1, 2, 3 }, list.ToArray());
    }

    [Fact]
    public void DefaultInstance_IsUsableAsReadOnlyList()
    {
        var list = default(EquatableReadOnlyList<int>);

        Assert.Empty(list);
    }

    [Fact]
    public void ImplementsReadOnlyListInterface()
    {
        var list = new EquatableReadOnlyList<string>(new[] { "a", "b" });

        IReadOnlyList<string> readOnly = list;
        Assert.Equal("a", readOnly[0]);
        Assert.Equal(2, readOnly.Count);
    }
}