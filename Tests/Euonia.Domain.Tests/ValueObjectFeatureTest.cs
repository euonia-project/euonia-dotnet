using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

public class TaggedDescriptor : ValueObject<TaggedDescriptor>
{
	public string Name { get; set; }

	public IReadOnlyList<int> Numbers { get; set; }
}

public class ValueObjectFeatureTest
{
	[Fact]
	public void Test_ToString_ContainsPropertyValues()
	{
		var value = new PersonDescriptor { Name = "foo", Age = 42 };

		Assert.NotEmpty(value.ToString());
		Assert.Contains("foo", value.ToString());
		Assert.Contains("42", value.ToString());
	}

	[Fact]
	public void Test_Equal_WhenCollectionContentsMatch()
	{
		var left = new TaggedDescriptor { Name = "tag", Numbers = new[] { 1, 2, 3 } };
		var right = new TaggedDescriptor { Name = "tag", Numbers = new[] { 1, 2, 3 } };

		Assert.True(left == right);
		Assert.True(left.Equals(right));
		Assert.Equal(left.GetHashCode(), right.GetHashCode());
	}

	[Fact]
	public void Test_NotEqual_WhenCollectionOrderDiffers()
	{
		var left = new TaggedDescriptor { Name = "tag", Numbers = new[] { 1, 2, 3 } };
		var right = new TaggedDescriptor { Name = "tag", Numbers = new[] { 3, 2, 1 } };

		Assert.False(left == right);
		Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
	}
}