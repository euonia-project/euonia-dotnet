// ReSharper disable All
using Xunit;

namespace Nerosoft.Euonia.Core.Tests;

public class ObjectIdTest
{
    [Fact]
    public void Equality_IntAndLongWithSameValueAreEqual()
    {
        ObjectId intId = new ObjectId(5);
        ObjectId longId = new ObjectId(5L);

        Assert.True(intId == longId);
        Assert.True(intId.Equals(longId));
        Assert.False(intId != longId);
    }

    [Fact]
    public void Equality_IntAndLongWithDifferentValuesAreNotEqual()
    {
        ObjectId intId = new ObjectId(5);
        ObjectId longId = new ObjectId(6L);

        Assert.False(intId == longId);
        Assert.True(intId != longId);
    }

    [Fact]
    public void GetHashCode_NormalizesNumericValues()
    {
        ObjectId intId = new ObjectId(42);
        ObjectId longId = new ObjectId(42L);

        Assert.Equal(intId.GetHashCode(), longId.GetHashCode());
    }

    [Fact]
    public void LongAndIntConversions_RoundTrip()
    {
        ObjectId id = new ObjectId(5L);

        long longValue = id;
        Assert.Equal(5L, longValue);

        int intValue = new ObjectId(42);
        Assert.Equal(42, intValue);
    }

    [Fact]
    public void StringRoundTrip_PreservesValue()
    {
        ObjectId id = new ObjectId("123456");

        string value = id;
        Assert.Equal("123456", value);
    }

    [Fact]
    public void EqualsMethod_ComparesBoxedNumericValues()
    {
        ObjectId id = new ObjectId(7L);

        Assert.True(id.Equals(new ObjectId(7)));
    }
}