// ReSharper disable All
using System;
using Nerosoft.Euonia.Collections;
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Collections;

/// <summary>
/// 验证 <see cref="PageableCollection{T}"/> 分页属性的计算规则，包括页数取整、起止位置与非法参数处理。
/// </summary>
public class PageableCollectionTest
{
    /// <summary>
    /// 验证总数为 10 且每页 5 条时页数为 2，整除时不会多出一页。
    /// </summary>
    [Fact]
    public void PageCount_CalculatesExactDivision()
    {
        var collection = new PageableCollection<int>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
        {
            TotalCount = 10,
            PageSize = 5
        };

        Assert.Equal(2, collection.PageCount);
    }

    /// <summary>
    /// 验证总数 11 且每页 5 条时页数为 3，余数会向上进位。
    /// </summary>
    [Fact]
    public void PageCount_RoundsUpRemainder()
    {
        var collection = new PageableCollection<int>(1, 2, 3)
        {
            TotalCount = 11,
            PageSize = 5
        };

        Assert.Equal(3, collection.PageCount);
    }

    /// <summary>
    /// 验证总数为 0 时页数为 0，空集合不会被算作一页。
    /// </summary>
    [Fact]
    public void PageCount_ReturnsZeroWhenTotalCountIsZero()
    {
        var collection = new PageableCollection<int>
        {
            TotalCount = 0,
            PageSize = 10
        };

        Assert.Equal(0, collection.PageCount);
    }

    /// <summary>
    /// 验证页大小为 0 时访问页数抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    [Fact]
    public void PageCount_ThrowsWhenPageSizeInvalid()
    {
        var collection = new PageableCollection<int> { PageSize = 0 };

        Assert.Throws<InvalidOperationException>(() => collection.PageCount);
    }

    /// <summary>
    /// 验证第 2 页、每页 5 条时起始位置为 6，即从 1 开始计数的页偏移加一。
    /// </summary>
    [Fact]
    public void StartPosition_IsZeroBasedOffsetPlusOne()
    {
        var collection = new PageableCollection<int>
        {
            PageNumber = 2,
            PageSize = 5
        };

        Assert.Equal(6, collection.StartPosition);
    }

    /// <summary>
    /// 验证页码为 0 时访问起始位置抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    [Fact]
    public void StartPosition_ThrowsWhenPageNumberInvalid()
    {
        var collection = new PageableCollection<int>
        {
            PageNumber = 0,
            PageSize = 5
        };

        Assert.Throws<InvalidOperationException>(() => collection.StartPosition);
    }

    /// <summary>
    /// 验证第 3 页越过末尾时结束位置被截断为总数 11。
    /// </summary>
    [Fact]
    public void EndPosition_ClampsAtTotalCount()
    {
        var collection = new PageableCollection<int>
        {
            PageNumber = 3,
            PageSize = 5,
            TotalCount = 11
        };

        Assert.Equal(11, collection.EndPosition);
    }

    /// <summary>
    /// 验证第 2 页未越过末尾时结束位置为页大小的倍数 10。
    /// </summary>
    [Fact]
    public void EndPosition_ReturnsPageSizeMultipleWithinTotal()
    {
        var collection = new PageableCollection<int>
        {
            PageNumber = 2,
            PageSize = 5,
            TotalCount = 11
        };

        Assert.Equal(10, collection.EndPosition);
    }

    /// <summary>
    /// 验证总数为 0 时结束位置为 0。
    /// </summary>
    [Fact]
    public void EndPosition_ReturnsZeroWhenTotalCountIsZero()
    {
        var collection = new PageableCollection<int>
        {
            PageNumber = 1,
            PageSize = 5,
            TotalCount = 0
        };

        Assert.Equal(0, collection.EndPosition);
    }
}