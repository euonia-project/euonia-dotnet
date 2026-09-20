// ReSharper disable All
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Nerosoft.Euonia.Core.Tests.Annotations;

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

    [Fact]
    public void ValidValues_Pass()
    {
        var attribute = new CollectionCountAttribute(1, 3);

        Assert.True(IsValid(attribute, new[] { "a" }));
        Assert.True(IsValid(attribute, new[] { "a", "b", "c" }));
    }

    [Fact]
    public void TooFewItems_Fail()
    {
        var attribute = new CollectionCountAttribute(3);

        Assert.False(IsValid(attribute, new[] { "a", "b" }));
    }

    [Fact]
    public void TooManyItems_Fail()
    {
        var attribute = new CollectionCountAttribute(1, 2);

        Assert.False(IsValid(attribute, new[] { "a", "b", "c" }));
    }

    [Fact]
    public void Null_IsValidByDefault()
    {
        var attribute = new CollectionCountAttribute(1);

        Assert.True(IsValid(attribute, null));
    }

    [Fact]
    public void Null_IsInvalidWhenAllowNullFalse()
    {
        var attribute = new CollectionCountAttribute(1)
        {
            AllowNull = false
        };

        Assert.False(IsValid(attribute, null));
    }

    [Fact]
    public void String_IsNotValidatedAsCollection()
    {
        var attribute = new CollectionCountAttribute(1, 2);

        Assert.True(IsValid(attribute, "abc"));
    }

    [Fact]
    public void NonCollectionValue_Fails()
    {
        var attribute = new CollectionCountAttribute(1);

        Assert.False(IsValid(attribute, 42));
    }

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

    [Fact]
    public void Constructor_ThrowsForNegativeMinimum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollectionCountAttribute(-1));
    }

    [Fact]
    public void Constructor_ThrowsWhenMaximumBelowMinimum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollectionCountAttribute(3, 2));
    }
}

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

    [Fact]
    public void ValidGuidString_Passes()
    {
        var attribute = new GuidAttribute();

        Assert.True(IsValid(attribute, "3F2504E0-4F89-41D3-9A0C-0305E82C3301"));
    }

    [Fact]
    public void InvalidGuidString_Fails()
    {
        var attribute = new GuidAttribute();

        Assert.False(IsValid(attribute, "not-a-guid"));
    }

    [Fact]
    public void NonEmptyGuidValue_Passes()
    {
        var attribute = new GuidAttribute();

        Assert.True(IsValid(attribute, Guid.NewGuid()));
    }

    [Fact]
    public void EmptyGuid_Fails()
    {
        var attribute = new GuidAttribute();

        Assert.False(IsValid(attribute, Guid.Empty));
    }

    [Fact]
    public void Null_IsValid()
    {
        var attribute = new GuidAttribute();

        Assert.True(IsValid(attribute, null));
    }
}