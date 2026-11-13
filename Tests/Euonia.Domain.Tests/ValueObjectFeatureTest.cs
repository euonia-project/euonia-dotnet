using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

/// <summary>
/// 用于验证集合属性相等性语义的带标签描述符。
/// </summary>
public class TaggedDescriptor : ValueObject<TaggedDescriptor>
{
	/// <summary>
	/// 获取或设置标签名称。
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 获取或设置与描述符关联的数字集合。
	/// </summary>
	public IReadOnlyList<int> Numbers { get; set; } = Array.Empty<int>();
}

/// <summary>
/// 验证值对象字符串表示与集合比较语义的测试集合。
/// </summary>
public class ValueObjectFeatureTest
{
	/// <summary>
	/// 值对象的字符串表示应包含其属性值。
	/// </summary>
	[Fact]
	public void Test_ToString_ContainsPropertyValues()
	{
		var value = new PersonDescriptor { Name = "foo", Age = 42 };

		Assert.NotEmpty(value.ToString());
		Assert.Contains("foo", value.ToString());
		Assert.Contains("42", value.ToString());
	}

	/// <summary>
	/// 当集合属性的元素与顺序均相同时，两个值对象应相等且哈希码一致。
	/// </summary>
	[Fact]
	public void Test_Equal_WhenCollectionContentsMatch()
	{
		var left = new TaggedDescriptor { Name = "tag", Numbers = new[] { 1, 2, 3 } };
		var right = new TaggedDescriptor { Name = "tag", Numbers = new[] { 1, 2, 3 } };

		Assert.True(left == right);
		Assert.True(left.Equals(right));
		Assert.Equal(left.GetHashCode(), right.GetHashCode());
	}

	/// <summary>
	/// 当集合属性元素相同但顺序不同时，两个值对象应不相等且哈希码不同。
	/// </summary>
	[Fact]
	public void Test_NotEqual_WhenCollectionOrderDiffers()
	{
		var left = new TaggedDescriptor { Name = "tag", Numbers = new[] { 1, 2, 3 } };
		var right = new TaggedDescriptor { Name = "tag", Numbers = new[] { 3, 2, 1 } };

		Assert.False(left == right);
		Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
	}
}