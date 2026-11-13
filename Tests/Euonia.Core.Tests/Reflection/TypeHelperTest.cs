using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Core.Tests.Reflection;

/// <summary>
/// 验证 <c>TypeHelper.CoerceValue</c> 将字符串转换为整数类型的结果及越界处理。
/// </summary>
public class TypeHelperTest
{
    /// <summary>
    /// 验证字符串 "1024" 可转换为 <see cref="int"/> 并得到 1024。
    /// </summary>
    [Fact]
    public void TestCoerceValueStringToInt32()
    {
        var result = TypeHelper.CoerceValue(typeof(int), typeof(string), "1024");
        Assert.Equal(1024, result);
    }

    /// <summary>
    /// 验证负数字符串 "-1024" 可转换为 <see cref="int"/> 并保留符号。
    /// </summary>
    [Fact]
    public void TestCoerceValueStringToInt32_Negative()
    {
        var result = TypeHelper.CoerceValue(typeof(int), typeof(string), "-1024");
        Assert.Equal(-1024, result);
    }

    /// <summary>
    /// 验证 <see cref="long"/> 最大值对应的字符串可转换并精确保留数值。
    /// </summary>
    [Fact]
    public void TestCoerceValueStringToInt64()
    {
        var result = TypeHelper.CoerceValue(typeof(long), typeof(string), "9223372036854775807");
        Assert.Equal(9223372036854775807, result);
    }

    /// <summary>
    /// 验证负的 <see cref="long"/> 大值字符串可转换并保留符号。
    /// </summary>
    [Fact]
    public void TestCoerceValueStringToInt64_Negative()
    {
        var result = TypeHelper.CoerceValue(typeof(long), typeof(string), "-9223372036854775807");
        Assert.Equal(-9223372036854775807, result);
    }

    /// <summary>
    /// 验证超出 <see cref="long"/> 范围的字符串转换时抛出 <see cref="ArgumentException"/>。
    /// </summary>
    [Fact]
    public void TestCoerceValueStringToInt64_ValueOutOfRange()
    {
        Assert.Throws<ArgumentException>(() => TypeHelper.CoerceValue(typeof(long), typeof(string), "9223372036854775810"));
    }
}
