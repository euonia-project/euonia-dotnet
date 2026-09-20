using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

public class PersonDescriptor : ValueObject<PersonDescriptor>
{
	public string Name { get; set; }

	public int Age { get; set; }
}

public class ValueObjectEqualityTest
{
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

	[Fact]
	public void Test_NotEqual_WhenOneSideHasNull_OtherHasValue()
	{
		var left = new PersonDescriptor { Name = null, Age = 42 };
		var right = new PersonDescriptor { Name = "foo", Age = 42 };

		Assert.False(left == right);
		Assert.False(left.Equals(right));
		Assert.True(left != right);
	}

	[Fact]
	public void Test_HashCode_ConsistentWithEquality()
	{
		var left = new PersonDescriptor { Name = null, Age = 1 };
		var right = new PersonDescriptor { Name = null, Age = 1 };

		Assert.Equal(left.GetHashCode(), right.GetHashCode());
	}

	[Fact]
	public void Test_EqualsObject_WithGenericBase_NoCastException()
	{
		var left = new PersonDescriptor { Name = "foo", Age = 42 };
		object baseValueObject = new ValueObject<PersonDescriptor>();

		Assert.False(left.Equals(baseValueObject));
	}

	[Fact]
	public void Test_Contains_InHashSet_WithNullProperties()
	{
		var item = new PersonDescriptor { Name = null, Age = 42 };
		var set = new HashSet<PersonDescriptor> { item };

		Assert.Contains(new PersonDescriptor { Name = null, Age = 42 }, set);
	}
}