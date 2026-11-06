// ReSharper disable All
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Annotations;

/// <summary>
/// 验证 <c>CollectionCountAttribute</c> 对集合元素数量的校验规则，包括上下限、空值与非法构造参数。
/// </summary>
public class CollectionCountAttributeTest
{
    private static bool IsValid(ValidationAttribute attribute, object value)
    {
        var validationResults = new List<ValidationResult>();
        return Validator.TryValidateValue(
            value,
            new ValidationContext(value ?? new object()) { MemberName = "TestProperty", DisplayName = "TestProperty" },
            validationResults,
            new[] { attribute });
    }

    /// <summary>
    /// 验证元素数量处于 1 至 3 之间（含边界）时校验通过。
    /// </summary>
    [Fact]
    public void ValidValues_Pass()
    {
        var attribute = new CollectionCountAttribute(1, 3);

        Assert.True(IsValid(attribute, new[] { "a" }));
        Assert.True(IsValid(attribute, new[] { "a", "b", "c" }));
    }

    /// <summary>
    /// 验证仅指定最小数量 3 时，元素数量为 2 的集合校验失败。
    /// </summary>
    [Fact]
    public void TooFewItems_Fail()
    {
        var attribute = new CollectionCountAttribute(3);

        Assert.False(IsValid(attribute, new[] { "a", "b" }));
    }

    /// <summary>
    /// 验证最大数量为 2 时，元素数量为 3 的集合校验失败。
    /// </summary>
    [Fact]
    public void TooManyItems_Fail()
    {
        var attribute = new CollectionCountAttribute(1, 2);

        Assert.False(IsValid(attribute, new[] { "a", "b", "c" }));
    }

    /// <summary>
    /// 验证默认允许空值时，<see langword="null"/> 校验通过。
    /// </summary>
    [Fact]
    public void Null_IsValidByDefault()
    {
        var attribute = new CollectionCountAttribute(1);

        Assert.True(IsValid(attribute, null!));
    }

    /// <summary>
    /// 验证将 <c>AllowNull</c> 设为 <see langword="false"/> 后，<see langword="null"/> 校验失败。
    /// </summary>
    [Fact]
    public void Null_IsInvalidWhenAllowNullFalse()
    {
        var attribute = new CollectionCountAttribute(1)
        {
            AllowNull = false
        };

        Assert.False(IsValid(attribute, null!));
    }

    /// <summary>
    /// 验证字符串不会被当作集合计数，即使长度超出上限也校验通过。
    /// </summary>
    [Fact]
    public void String_IsNotValidatedAsCollection()
    {
        var attribute = new CollectionCountAttribute(1, 2);

        Assert.True(IsValid(attribute, "abc"));
    }

    /// <summary>
    /// 验证非集合值（如整数）无法计数，校验失败。
    /// </summary>
    [Fact]
    public void NonCollectionValue_Fails()
    {
        var attribute = new CollectionCountAttribute(1);

        Assert.False(IsValid(attribute, 42));
    }

    /// <summary>
    /// 验证没有 <c>Count</c> 属性的可枚举对象会被实际枚举并计数后完成校验。
    /// </summary>
    [Fact]
    public void EnumerableWithoutCountProperty_IsEnumerated()
    {
        var attribute = new CollectionCountAttribute(1, 2);
        static IEnumerable<int> Items()
        {
            yield return 1;
            yield return 2;
        }

        Assert.True(IsValid(attribute, Items()));
    }

    /// <summary>
    /// 验证最小数量为负数时构造函数抛出 <see cref="ArgumentOutOfRangeException"/>。
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNegativeMinimum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollectionCountAttribute(-1));
    }

    /// <summary>
    /// 验证最大数量小于最小数量时构造函数抛出 <see cref="ArgumentOutOfRangeException"/>。
    /// </summary>
    [Fact]
    public void Constructor_ThrowsWhenMaximumBelowMinimum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollectionCountAttribute(3, 2));
    }
}

/// <summary>
/// 验证 <c>GuidAttribute</c> 对 Guid 字符串与 Guid 值的校验规则。
/// </summary>
public class GuidAttributeTest
{
    private static bool IsValid(ValidationAttribute attribute, object value)
    {
        var validationResults = new List<ValidationResult>();
        return Validator.TryValidateValue(
            value,
            new ValidationContext(value ?? new object()) { MemberName = "TestProperty", DisplayName = "TestProperty" },
            validationResults,
            new[] { attribute });
    }

    /// <summary>
    /// 验证格式合法的 Guid 字符串校验通过。
    /// </summary>
    [Fact]
    public void ValidGuidString_Passes()
    {
        var attribute = new GuidAttribute();

        Assert.True(IsValid(attribute, "3F2504E0-4F89-41D3-9A0C-0305E82C3301"));
    }

    /// <summary>
    /// 验证无法解析为 Guid 的字符串校验失败。
    /// </summary>
    [Fact]
    public void InvalidGuidString_Fails()
    {
        var attribute = new GuidAttribute();

        Assert.False(IsValid(attribute, "not-a-guid"));
    }

    /// <summary>
    /// 验证非空的 <see cref="Guid"/> 值校验通过。
    /// </summary>
    [Fact]
    public void NonEmptyGuidValue_Passes()
    {
        var attribute = new GuidAttribute();

        Assert.True(IsValid(attribute, Guid.NewGuid()));
    }

    /// <summary>
    /// 验证 <see cref="Guid.Empty"/> 校验失败。
    /// </summary>
    [Fact]
    public void EmptyGuid_Fails()
    {
        var attribute = new GuidAttribute();

        Assert.False(IsValid(attribute, Guid.Empty));
    }

    /// <summary>
    /// 验证 <see langword="null"/> 校验通过，空值交由其他规则处理。
    /// </summary>
    [Fact]
    public void Null_IsValid()
    {
        var attribute = new GuidAttribute();

        Assert.True(IsValid(attribute, null!));
    }
}