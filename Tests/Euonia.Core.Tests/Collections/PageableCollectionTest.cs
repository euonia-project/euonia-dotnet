// ReSharper disable All
using System;
using Nerosoft.Euonia.Collections;
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Collections;

public class PageableCollectionTest
{
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

    [Fact]
    public void PageCount_ThrowsWhenPageSizeInvalid()
    {
        var collection = new PageableCollection<int> { PageSize = 0 };

        Assert.Throws<InvalidOperationException>(() => collection.PageCount);
    }

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