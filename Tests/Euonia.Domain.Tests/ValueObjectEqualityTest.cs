using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

/// <summary>
/// 用于验证值对象相等性语义的人员描述符。
/// </summary>
public class PersonDescriptor : ValueObject<PersonDescriptor>
{
	/// <summary>
	/// 获取或设置人员姓名。
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// 获取或设置人员年龄。
	/// </summary>
	public int Age { get; set; }
}

/// <summary>
/// 验证 <see cref="ValueObject{T}"/> 相等性语义的测试集合。
/// </summary>
public class ValueObjectEqualityTest
{
	/// <summary>
	/// 当所有属性值相同（包括空引用属性）时，两个值对象应相等，且相等运算符与哈希码结果一致。
	/// </summary>
	[Fact]
	public void Test_Equal_WhenAllPropertiesMatch_IncludingNulls()
	{
		var left = new PersonDescriptor { Name = null, Age = 42 };
		var right = new PersonDescriptor { Name = null, Age = 42 };

		Assert.True(left == right);
		Assert.True(left.Equals(right));
		Assert.True(left.Equals((object)right));
		Assert.False(left != right);
	}

	/// <summary>
	/// 当一侧属性为空引用而另一侧有值时，两个值对象应不相等。
	/// </summary>
	[Fact]
	public void Test_NotEqual_WhenOneSideHasNull_OtherHasValue()
	{
		var left = new PersonDescriptor { Name = null, Age = 42 };
		var right = new PersonDescriptor { Name = "foo", Age = 42 };

		Assert.False(left == right);
		Assert.False(left.Equals(right));
		Assert.True(left != right);
	}

	/// <summary>
	/// 相等的值对象应返回相同的哈希码。
	/// </summary>
	[Fact]
	public void Test_HashCode_ConsistentWithEquality()
	{
		var left = new PersonDescriptor { Name = null, Age = 1 };
		var right = new PersonDescriptor { Name = null, Age = 1 };

		Assert.Equal(left.GetHashCode(), right.GetHashCode());
	}

	/// <summary>
	/// 与非同类型的值对象基类实例比较时应返回 <see langword="false"/>，且不抛出类型转换异常。
	/// </summary>
	[Fact]
	public void Test_EqualsObject_WithGenericBase_NoCastException()
	{
		var left = new PersonDescriptor { Name = "foo", Age = 42 };
		object baseValueObject = new ValueObject<PersonDescriptor>();

		Assert.False(left.Equals(baseValueObject));
	}

	/// <summary>
	/// 包含空引用属性的值对象应能作为哈希集合的元素被正确查找。
	/// </summary>
	[Fact]
	public void Test_Contains_InHashSet_WithNullProperties()
	{
		var item = new PersonDescriptor { Name = null, Age = 42 };
		var set = new HashSet<PersonDescriptor> { item };

		Assert.Contains(new PersonDescriptor { Name = null, Age = 42 }, set);
	}
}