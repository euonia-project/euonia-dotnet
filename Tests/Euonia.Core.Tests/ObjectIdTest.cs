// ReSharper disable All
using Xunit;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证 <see cref="ObjectId"/> 在整型与长整型取值、哈希码、类型转换及字符串表示上的一致性。
/// </summary>
public class ObjectIdTest
{
    /// <summary>
    /// 验证分别用 int 与 long 构造但数值相同的 <see cref="ObjectId"/> 判定为相等。
    /// </summary>
    [Fact]
    public void Equality_IntAndLongWithSameValueAreEqual()
    {
        ObjectId intId = new ObjectId(5);
        ObjectId longId = new ObjectId(5L);

        Assert.True(intId == longId);
        Assert.True(intId.Equals(longId));
        Assert.False(intId != longId);
    }

    /// <summary>
    /// 验证数值不同的 <see cref="ObjectId"/> 判定为不相等。
    /// </summary>
    [Fact]
    public void Equality_IntAndLongWithDifferentValuesAreNotEqual()
    {
        ObjectId intId = new ObjectId(5);
        ObjectId longId = new ObjectId(6L);

        Assert.False(intId == longId);
        Assert.True(intId != longId);
    }

    /// <summary>
    /// 验证相同数值由 int 与 long 构造后生成相同的哈希码。
    /// </summary>
    [Fact]
    public void GetHashCode_NormalizesNumericValues()
    {
        ObjectId intId = new ObjectId(42);
        ObjectId longId = new ObjectId(42L);

        Assert.Equal(intId.GetHashCode(), longId.GetHashCode());
    }

    /// <summary>
    /// 验证到 long 与 int 的隐式转换分别保留原有数值。
    /// </summary>
    [Fact]
    public void LongAndIntConversions_RoundTrip()
    {
        ObjectId id = new ObjectId(5L);

        long longValue = id;
        Assert.Equal(5L, longValue);

        int intValue = new ObjectId(42);
        Assert.Equal(42, intValue);
    }

    /// <summary>
    /// 验证由字符串构造的实例可通过隐式转换还原出原始字符串。
    /// </summary>
    [Fact]
    public void StringRoundTrip_PreservesValue()
    {
        ObjectId id = new ObjectId("123456");

        string value = id;
        Assert.Equal("123456", value);
    }

    /// <summary>
    /// 验证 <see cref="object.Equals(object)"/> 以装箱的数值进行比较，而非比较引用。
    /// </summary>
    [Fact]
    public void EqualsMethod_ComparesBoxedNumericValues()
    {
        ObjectId id = new ObjectId(7L);

        Assert.True(id.Equals(new ObjectId(7)));
    }
}