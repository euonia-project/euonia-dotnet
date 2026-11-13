using Nerosoft.Euonia.Linq;

namespace Nerosoft.Euonia.Linq.Tests;

/// <summary>
/// 验证 PredicateBuilder 的谓词构造方法。
/// </summary>
public class PredicateBuilderTests
{
	/// <summary>
	/// 谓词构造测试用元素类型。
	/// </summary>
	private class Item
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;
	}

	/// <summary>
	/// True 生成的谓词对任意元素都应返回 <see langword="true"/>。
	/// </summary>
	[Fact]
	public void True_ShouldAlwaysMatch()
	{
		var predicate = PredicateBuilder.True<Item>().Compile();

		Assert.True(predicate(new Item { Id = 1 }));
	}

	/// <summary>
	/// False 生成的谓词对任意元素都应返回 <see langword="false"/>。
	/// </summary>
	[Fact]
	public void False_ShouldNeverMatch()
	{
		var predicate = PredicateBuilder.False<Item>().Compile();

		Assert.False(predicate(new Item { Id = 1 }));
	}

	/// <summary>
	/// 属性相等谓词应仅匹配属性值等于目标值的元素。
	/// </summary>
	[Fact]
	public void PropertyEqual_ShouldMatch()
	{
		var predicate = PredicateBuilder.PropertyEqual<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.True(predicate(new Item { Id = 5 }));
		Assert.False(predicate(new Item { Id = 6 }));
	}

	/// <summary>
	/// 属性不等谓词应仅匹配属性值不等于目标值的元素。
	/// </summary>
	[Fact]
	public void PropertyNotEqual_ShouldMatch()
	{
		var predicate = PredicateBuilder.PropertyNotEqual<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.True(predicate(new Item { Id = 6 }));
		Assert.False(predicate(new Item { Id = 5 }));
	}

	/// <summary>
	/// 属性大于谓词应仅对属性值大于目标值的元素返回 <see langword="true"/>。
	/// </summary>
	/// <param name="id">被测元素的属性值。</param>
	/// <param name="expected">预期匹配结果。</param>
	[Theory]
	[InlineData(6, true)]
	[InlineData(5, false)]
	[InlineData(4, false)]
	public void PropertyGreaterThan_ShouldMatch(int id, bool expected)
	{
		var predicate = PredicateBuilder.PropertyGreaterThan<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.Equal(expected, predicate(new Item { Id = id }));
	}

	/// <summary>
	/// 属性大于等于谓词应仅对属性值不小于目标值的元素返回 <see langword="true"/>。
	/// </summary>
	/// <param name="id">被测元素的属性值。</param>
	/// <param name="expected">预期匹配结果。</param>
	[Theory]
	[InlineData(5, true)]
	[InlineData(4, false)]
	public void PropertyGreaterThanOrEqual_ShouldMatch(int id, bool expected)
	{
		var predicate = PredicateBuilder.GreaterThanOrEqual<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.Equal(expected, predicate(new Item { Id = id }));
	}

	/// <summary>
	/// 属性小于谓词应仅对属性值小于目标值的元素返回 <see langword="true"/>。
	/// </summary>
	/// <param name="id">被测元素的属性值。</param>
	/// <param name="expected">预期匹配结果。</param>
	[Theory]
	[InlineData(4, true)]
	[InlineData(5, false)]
	public void PropertyLessThan_ShouldMatch(int id, bool expected)
	{
		var predicate = PredicateBuilder.PropertyLessThan<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.Equal(expected, predicate(new Item { Id = id }));
	}

	/// <summary>
	/// 属性小于等于谓词应仅对属性值不大于目标值的元素返回 <see langword="true"/>。
	/// </summary>
	/// <param name="id">被测元素的属性值。</param>
	/// <param name="expected">预期匹配结果。</param>
	[Theory]
	[InlineData(5, true)]
	[InlineData(6, false)]
	public void PropertyLessThanOrEqual_ShouldMatch(int id, bool expected)
	{
		var predicate = PredicateBuilder.PropertyLessThanOrEqual<Item, int>(nameof(Item.Id), 5).Compile();

		Assert.Equal(expected, predicate(new Item { Id = id }));
	}

	/// <summary>
	/// 按相等运算符构造比较条件时应生成仅匹配目标值的谓词。
	/// </summary>
	[Fact]
	public void GetCompareCondition_Equal_ShouldMatch()
	{
		var predicate = PredicateBuilder.GetCompareCondition<Item, int>(null!, nameof(Item.Id), 7, QueryOperator.Equal).Compile();

		Assert.True(predicate(new Item { Id = 7 }));
		Assert.False(predicate(new Item { Id = 8 }));
	}

	/// <summary>
	/// 包含条件谓词应仅匹配属性值存在于给定集合中的元素。
	/// </summary>
	[Fact]
	public void GetContainsCondition_ShouldMatch()
	{
		var predicate = PredicateBuilder.GetContainsCondition<Item, int>(null!, nameof(Item.Id), new List<int> { 1, 3, 5 }).Compile();

		Assert.True(predicate(new Item { Id = 3 }));
		Assert.False(predicate(new Item { Id = 2 }));
	}

	/// <summary>
	/// 区间谓词应仅匹配落入指定范围或等于边界值之一的元素。
	/// </summary>
	[Fact]
	public void PropertyInRange_ShouldMatch()
	{
		var predicate = PredicateBuilder.PropertyInRange<Item, int>(nameof(Item.Id), 2, 4, 6).Compile();

		Assert.True(predicate(new Item { Id = 4 }));
		Assert.False(predicate(new Item { Id = 5 }));
	}
}
