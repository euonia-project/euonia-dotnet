using Nerosoft.Euonia.Linq;

namespace Nerosoft.Euonia.Linq.Tests;

/// <summary>
/// 验证 PredicateBuilder 的谓词构造方法。
/// </summary>
public class PredicateBuilderTests
{
	private class Item
	{
		public int Id { get; set; }

		public string Name { get; set; }
	}

	[Fact]
	public void True_ShouldAlwaysMatch()
	{
		var predicate = PredicateBuilder.True<Item>().Compile();

		Assert.True(predicate(new Item { Id = 1 }));
	}

	[Fact]
	public void False_ShouldNeverMatch()
	{
		var predicate = PredicateBuilder.False<Item>().Compile();

		Assert.False(predicate(new Item { Id = 1 }));
	}

	[Fact]
	public void PropertyEqual_ShouldMatch()
	{
		var predicate = PredicateBuilder.PropertyEqual<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.True(predicate(new Item { Id = 5 }));
		Assert.False(predicate(new Item { Id = 6 }));
	}

	[Fact]
	public void PropertyNotEqual_ShouldMatch()
	{
		var predicate = PredicateBuilder.PropertyNotEqual<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.True(predicate(new Item { Id = 6 }));
		Assert.False(predicate(new Item { Id = 5 }));
	}

	[Theory]
	[InlineData(6, true)]
	[InlineData(5, false)]
	[InlineData(4, false)]
	public void PropertyGreaterThan_ShouldMatch(int id, bool expected)
	{
		var predicate = PredicateBuilder.PropertyGreaterThan<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.Equal(expected, predicate(new Item { Id = id }));
	}

	[Theory]
	[InlineData(5, true)]
	[InlineData(4, false)]
	public void PropertyGreaterThanOrEqual_ShouldMatch(int id, bool expected)
	{
		var predicate = PredicateBuilder.GreaterThanOrEqual<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.Equal(expected, predicate(new Item { Id = id }));
	}

	[Theory]
	[InlineData(4, true)]
	[InlineData(5, false)]
	public void PropertyLessThan_ShouldMatch(int id, bool expected)
	{
		var predicate = PredicateBuilder.PropertyLessThan<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.Equal(expected, predicate(new Item { Id = id }));
	}

	[Theory]
	[InlineData(5, true)]
	[InlineData(6, false)]
	public void PropertyLessThanOrEqual_ShouldMatch(int id, bool expected)
	{
		var predicate = PredicateBuilder.PropertyLessThanOrEqual<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.Equal(expected, predicate(new Item { Id = id }));
	}

	[Fact]
	public void GetCompareCondition_Equal_ShouldMatch()
	{
		var predicate = PredicateBuilder.GetCompareCondition<Item, int>(null, nameof(Item.Id), 7, QueryOperator.Equal).Compile();

		Assert.True(predicate(new Item { Id = 7 }));
		Assert.False(predicate(new Item { Id = 8 }));
	}

	[Fact]
	public void GetContainsCondition_ShouldMatch()
	{
		var predicate = PredicateBuilder.GetContainsCondition<Item, int>(null, nameof(Item.Id), new List<int> { 1, 3, 5 }).Compile();

		Assert.True(predicate(new Item { Id = 3 }));
		Assert.False(predicate(new Item { Id = 2 }));
	}

	[Fact]
	public void PropertyInRange_ShouldMatch()
	{
		var predicate = PredicateBuilder.PropertyInRange<Item, int>(nameof(Item.Id), 2, 4, 6).Compile();

		Assert.True(predicate(new Item { Id = 4 }));
		Assert.False(predicate(new Item { Id = 5 }));
	}
}
