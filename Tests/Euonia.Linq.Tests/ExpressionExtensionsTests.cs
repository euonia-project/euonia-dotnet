using System.Linq.Expressions;
using Nerosoft.Euonia.Linq;

namespace Nerosoft.Euonia.Linq.Tests;

/// <summary>
/// 验证表达式组合扩展方法（Compose、And、Or、Not、Extend）的行为。
/// </summary>
public class ExpressionExtensionsTests
{
	private class Item
	{
		public int Id { get; set; }

		public string Name { get; set; }
	}

	private static List<Item> CreateItems()
	{
		return Enumerable.Range(1, 10).Select(x => new Item { Id = x, Name = "n" + x }).ToList();
	}

	private static List<int> Apply(IEnumerable<Item> items, Expression<Func<Item, bool>> predicate)
	{
		return items.Where(predicate.Compile()).Select(x => x.Id).ToList();
	}

	[Fact]
	public void Compose_OrElse_ShouldCombineWithOr()
	{
		// 回归测试：修复前 OrElse 使用 True 种子，结果为恒真表达式
		var predicates = new List<Expression<Func<Item, bool>>>
		{
			x => x.Id == 1,
			x => x.Id == 5,
			x => x.Id == 9
		};

		var result = Apply(CreateItems(), predicates.Compose(PredicateOperator.OrElse));

		Assert.Equal(new[] { 1, 5, 9 }, result);
	}

	[Fact]
	public void Compose_AndAlso_ShouldCombineWithAnd()
	{
		var predicates = new List<Expression<Func<Item, bool>>>
		{
			x => x.Id > 1,
			x => x.Id < 9,
			x => x.Id % 2 == 0
		};

		var result = Apply(CreateItems(), predicates.Compose());

		Assert.Equal(new[] { 2, 4, 6, 8 }, result);
	}

	[Fact]
	public void Compose_SingleExpression_ShouldReturnIt()
	{
		var predicate = new List<Expression<Func<Item, bool>>> { x => x.Id == 3 };

		var result = Apply(CreateItems(), predicate.Compose(PredicateOperator.OrElse));

		Assert.Equal(new[] { 3 }, result);
	}

	[Fact]
	public void Compose_Empty_AndAlso_ShouldReturnTrue()
	{
		// 恒等元素：空集合 AND 组合应匹配全部
		var result = Apply(CreateItems(), Enumerable.Empty<Expression<Func<Item, bool>>>().Compose());

		Assert.Equal(10, result.Count);
	}

	[Fact]
	public void Compose_Empty_OrElse_ShouldReturnFalse()
	{
		// 恒等元素：空集合 OR 组合应匹配空集
		var result = Apply(CreateItems(), Enumerable.Empty<Expression<Func<Item, bool>>>().Compose(PredicateOperator.OrElse));

		Assert.Empty(result);
	}

	[Fact]
	public void And_ShouldCombinePredicates()
	{
		var result = Apply(CreateItems(), PredicateBuilder.True<Item>().And(x => x.Id > 5));

		Assert.Equal(new[] { 6, 7, 8, 9, 10 }, result);
	}

	[Fact]
	public void Or_ShouldCombinePredicates()
	{
		var result = Apply(CreateItems(), PredicateBuilder.False<Item>().Or(x => x.Id == 7));

		Assert.Equal(new[] { 7 }, result);
	}

	[Fact]
	public void Not_ShouldNegatePredicate()
	{
		var result = Apply(CreateItems(), PredicateBuilder.True<Item>().Not());

		Assert.Empty(result);
	}

	[Fact]
	public void Extend_AndAlso_ShouldCombineWithAnd()
	{
		var result = Apply(CreateItems(), PredicateBuilder.True<Item>().Extend(x => x.Id <= 3));

		Assert.Equal(new[] { 1, 2, 3 }, result);
	}

	[Fact]
	public void Extend_OrElse_ShouldCombineWithOr()
	{
		var result = Apply(CreateItems(), PredicateBuilder.False<Item>().Extend(x => x.Id == 2, PredicateOperator.OrElse));

		Assert.Equal(new[] { 2 }, result);
	}
}
