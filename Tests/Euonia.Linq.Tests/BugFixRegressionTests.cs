using System.Linq.Expressions;
using Nerosoft.Euonia.Linq;

namespace Nerosoft.Euonia.Linq.Tests;

/// <summary>
/// 验证 QueryHandler、Lambda 条件计数与可空属性比较等缺陷修复的回归行为。
/// </summary>
public class BugFixRegressionTests
{
	private class Item
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public int? Score { get; set; }
	}

	private static List<Item> CreateItems()
	{
		return Enumerable.Range(1, 10).Select(x => new Item { Id = x, Name = "n" + x, Score = x }).ToList();
	}

	[Fact]
	public void QueryHandler_QueryThenGetCount_ShouldCountUnpagedResults()
	{
		// 回归测试：修复前 GetCount 会基于已分页的 _query 再次过滤，导致计数错误
		var handler = new QueryHandler<Item>(CreateItems().AsQueryable())
			.AddCriteria(x => x.Id > 2)
			.SetPage(1)
			.SetSize(3);

		var page = handler.Query();
		var count = handler.GetCount();

		Assert.Equal(new[] { 3, 4, 5 }, page.Select(x => x.Id));
		Assert.Equal(8, count);
	}

	[Fact]
	public void QueryHandler_QueryTwice_ShouldNotStackFilters()
	{
		// 回归测试：修复前重复调用 Query 会重复叠加过滤条件
		var handler = new QueryHandler<Item>(CreateItems().AsQueryable())
			.AddCriteria(x => x.Id > 5)
			.SetPage(2)
			.SetSize(2);

		var first = handler.Query();
		var second = handler.Query();

		// 过滤后为 [6,7,8,9,10]，第 2 页每页 2 条为 [8,9]
		Assert.Equal(new[] { 8, 9 }, first.Select(x => x.Id));
		Assert.Equal(new[] { 8, 9 }, second.Select(x => x.Id));
	}

	[Fact]
	public void QueryHandler_SetPage_ShouldRejectInvalidPage()
	{
		var handler = new QueryHandler<Item>(CreateItems().AsQueryable());

		Assert.Throws<ArgumentOutOfRangeException>(() => handler.SetPage(0));
	}

	[Fact]
	public void GetConditionCount_ShouldIgnoreAndAlsoInStringLiteral()
	{
		// 回归测试：修复前基于字符串解析，字面量含 AndAlso 会被误计数
		Expression<Func<Item, bool>> predicate = x => x.Name.Contains("AndAlso");

		Assert.Equal(1, Lambda.GetConditionCount(predicate));
	}

	[Fact]
	public void GetConditionCount_ShouldCountLogicalOperators()
	{
		Expression<Func<Item, bool>> predicate = x => x.Id > 1 && x.Id < 9 || x.Name == "n5";

		Assert.Equal(3, Lambda.GetConditionCount(predicate));
	}

	[Fact]
	public void PredicateBuilder_PropertyEqual_NullableShouldMatch()
	{
		// 回归测试：修复前 Expression.Constant(int, typeof(int?)) 抛出 ArgumentException
		var predicate = PredicateBuilder.PropertyEqual<Item, int>(nameof(Item.Score), 5).Compile();

		Assert.True(predicate(new Item { Id = 1, Score = 5 }));
		Assert.False(predicate(new Item { Id = 2, Score = 6 }));
	}

	[Fact]
	public void PredicateBuilder_GetCompareCondition_NullableShouldMatch()
	{
		var predicate = PredicateBuilder.GetCompareCondition<Item, int>(null, nameof(Item.Score), 5, QueryOperator.GreaterThan).Compile();

		Assert.True(predicate(new Item { Id = 1, Score = 6 }));
		Assert.False(predicate(new Item { Id = 2, Score = 4 }));
	}

	[Fact]
	public void PredicateBuilder_PropertyInRange_ShouldMatch()
	{
		// 回归测试：修复前判空发生在 MakeGenericMethod 之后，空引用检查失效
		var predicate = PredicateBuilder.PropertyInRange<Item, int>(nameof(Item.Id), 2, 4, 6).Compile();

		Assert.True(predicate(new Item { Id = 4 }));
		Assert.False(predicate(new Item { Id = 5 }));
	}

	[Fact]
	public void Expression_Operation_NotContains_ShouldNegateContains()
	{
		var parameter = Expression.Parameter(typeof(Item), "t");
		var expression = parameter.Property(nameof(Item.Name))
		                          .Operation(QueryOperator.NotContains, "n1")
		                          .ToLambda<Func<Item, bool>>(parameter)
		                          .Compile();

		Assert.True(expression(new Item { Id = 1, Name = "abc" }));
		Assert.False(expression(new Item { Id = 2, Name = "n1x" }));
	}

	[Fact]
	public void Expression_Operation_Is_ShouldCheckNull()
	{
		var parameter = Expression.Parameter(typeof(Item), "t");
		var expression = parameter.Property(nameof(Item.Name))
		                          .Operation(QueryOperator.Is, null)
		                          .ToLambda<Func<Item, bool>>(parameter)
		                          .Compile();

		Assert.True(expression(new Item { Id = 1, Name = null }));
		Assert.False(expression(new Item { Id = 2, Name = "n2" }));
	}

	[Fact]
	public void PredicateExpressionBuilder_Empty_ShouldReturnTruePredicate()
	{
		// 回归测试：修复前空构建器返回 null，下游组合时触发空引用
		var builder = new PredicateExpressionBuilder<Item>();

		var predicate = builder.ToLambda().Compile();

		Assert.True(predicate(new Item { Id = 1 }));
	}

	[Fact]
	public void Queryable_Where_WithNullableProperty_ShouldFilter()
	{
		var items = new List<Item>
		{
			new() { Id = 1, Score = 10 },
			new() { Id = 2, Score = null }
		};

		var result = items.AsQueryable().Where<Item>(nameof(Item.Score), 10).ToList();

		Assert.Single(result);
		Assert.Equal(1, result[0].Id);
	}
}
