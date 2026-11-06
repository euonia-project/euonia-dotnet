using System.Linq.Expressions;
using Nerosoft.Euonia.Linq;

namespace Nerosoft.Euonia.Linq.Tests;

/// <summary>
/// 验证 QueryHandler、Lambda 条件计数与可空属性比较等缺陷修复的回归行为。
/// </summary>
public class BugFixRegressionTests
{
	/// <summary>
	/// 带可空 <see cref="Score"/> 的查询元素，用于验证过滤与排序的组合行为。
	/// </summary>
	private class Item
	{
		public int Id { get; set; }

		public string Name { get; set; } = null!;

		public int? Score { get; set; }
	}

	private static List<Item> CreateItems()
	{
		return Enumerable.Range(1, 10).Select(x => new Item { Id = x, Name = "n" + x, Score = x }).ToList();
	}

	/// <summary>
	/// 排序键所属对象，用于验证排序在过滤之后执行时，已被谓词排除的空引用行不会参与求值。
	/// </summary>
	private class Owner
	{
		public string Name { get; set; } = string.Empty;
	}

	/// <summary>
	/// 带 <see cref="Owner"/> 导航属性的查询元素，其中导航属性允许为 <see langword="null"/>。
	/// </summary>
	private class OwnedItem
	{
		public int Id { get; set; }

		public Owner Owner { get; set; } = null!;
	}

	/// <summary>
	/// 向查询处理器传入空条件时应抛出 <see cref="ArgumentNullException"/>，避免过滤条件被静默忽略。
	/// </summary>
	[Fact]
	public void QueryHandler_AddCriteria_Null_ShouldThrow()
	{
		// 回归测试：null 谓词在组合时被当作恒真条件忽略，会让查询静默失去过滤条件。
		var handler = new QueryHandler<Item>(CreateItems().AsQueryable());

		Assert.Throws<ArgumentNullException>(() => handler.AddCriteria(null!));
	}

	/// <summary>
	/// 排序应在过滤之后执行，避免对已被谓词排除的行求值排序键而抛出空引用异常。
	/// </summary>
	[Fact]
	public void QueryHandler_OrderBy_ShouldFilterBeforeSorting()
	{
		// 回归测试：排序曾直接作用于原始查询，LINQ to Objects 会对随后被谓词排除的行求值排序键，
		// 当排序键路径包含 null 时会在排序阶段抛 NullReferenceException。
		var items = new List<OwnedItem>
		{
			new() { Id = 1, Owner = null! },
			new() { Id = 2, Owner = new Owner { Name = "b" } },
			new() { Id = 3, Owner = new Owner { Name = "a" } }
		};

		var result = new QueryHandler<OwnedItem>(items.AsQueryable())
			.AddCriteria(x => x.Owner != null)
			.OrderByAscending(x => x.Owner.Name)
			.Query();

		Assert.Equal(new[] { 3, 2 }, result.Select(x => x.Id));
	}

	/// <summary>
	/// 嵌套属性表达式应生成作用于完整成员路径的谓词，而非仅绑定叶成员。
	/// </summary>
	[Fact]
	public void PredicateExpressionBuilder_NestedProperty_ShouldBuildPredicate()
	{
		// 回归测试：旧实现只取叶成员并挂到 TEntity 参数上，x => x.Owner.Name 会被当成 TEntity.Name。
		var builder = new PredicateExpressionBuilder<OwnedItem>();
		builder.Append(x => x.Owner.Name, QueryOperator.Equal, "a");

		var predicate = builder.ToLambda().Compile();

		Assert.True(predicate(new OwnedItem { Owner = new Owner { Name = "a" } }));
		Assert.False(predicate(new OwnedItem { Owner = new Owner { Name = "b" } }));
	}

	/// <summary>
	/// 使用空查询源构造查询处理器时应抛出 <see cref="ArgumentNullException"/>。
	/// </summary>
	[Fact]
	public void QueryHandler_NullQuery_ShouldThrow()
	{
		Assert.Throws<ArgumentNullException>(() => new QueryHandler<Item>(null!));
	}

	/// <summary>
	/// 为查询处理器设置空排序委托时应抛出 <see cref="ArgumentNullException"/>。
	/// </summary>
	[Fact]
	public void QueryHandler_SetCollator_Null_ShouldThrow()
	{
		var handler = new QueryHandler<Item>(CreateItems().AsQueryable());

		Assert.Throws<ArgumentNullException>(() => handler.SetCollator((Action<Orderable<Item>>)null!));
		Assert.Throws<ArgumentNullException>(() => handler.SetCollator((Func<IQueryable<Item>, IOrderedQueryable<Item>>)null!));
	}

	/// <summary>
	/// 先分页查询再获取总数时，计数应基于未分页的过滤结果集。
	/// </summary>
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

	/// <summary>
	/// 重复执行查询不应重复叠加过滤条件，两次结果应保持一致。
	/// </summary>
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

	/// <summary>
	/// 页码小于 1 时应抛出 <see cref="ArgumentOutOfRangeException"/>。
	/// </summary>
	[Fact]
	public void QueryHandler_SetPage_ShouldRejectInvalidPage()
	{
		var handler = new QueryHandler<Item>(CreateItems().AsQueryable());

		Assert.Throws<ArgumentOutOfRangeException>(() => handler.SetPage(0));
	}

	/// <summary>
	/// 统计条件数量时应忽略字符串字面量中的逻辑运算符文本。
	/// </summary>
	[Fact]
	public void GetConditionCount_ShouldIgnoreAndAlsoInStringLiteral()
	{
		// 回归测试：修复前基于字符串解析，字面量含 AndAlso 会被误计数
		Expression<Func<Item, bool>> predicate = x => x.Name.Contains("AndAlso");

		Assert.Equal(1, Lambda.GetConditionCount(predicate));
	}

	/// <summary>
	/// 统计条件数量时应将 AndAlso 与 OrElse 等逻辑运算符计入。
	/// </summary>
	[Fact]
	public void GetConditionCount_ShouldCountLogicalOperators()
	{
		Expression<Func<Item, bool>> predicate = x => x.Id > 1 && x.Id < 9 || x.Name == "n5";

		Assert.Equal(3, Lambda.GetConditionCount(predicate));
	}

	/// <summary>
	/// 对可空属性执行相等比较时应生成可正常求值的谓词。
	/// </summary>
	[Fact]
	public void PredicateBuilder_PropertyEqual_NullableShouldMatch()
	{
		// 回归测试：修复前 Expression.Constant(int, typeof(int?)) 抛出 ArgumentException
		var predicate = PredicateBuilder.PropertyEqual<Item, int>(nameof(Item.Score), 5).Compile();

		Assert.True(predicate(new Item { Id = 1, Score = 5 }));
		Assert.False(predicate(new Item { Id = 2, Score = 6 }));
	}

	/// <summary>
	/// 对可空属性构造比较条件时应生成可正常求值的谓词。
	/// </summary>
	[Fact]
	public void PredicateBuilder_GetCompareCondition_NullableShouldMatch()
	{
		var predicate = PredicateBuilder.GetCompareCondition<Item, int>(null!, nameof(Item.Score), 5, QueryOperator.GreaterThan).Compile();

		Assert.True(predicate(new Item { Id = 1, Score = 6 }));
		Assert.False(predicate(new Item { Id = 2, Score = 4 }));
	}

	/// <summary>
	/// 区间判断应仅匹配落入指定范围或等于边界值之一的元素。
	/// </summary>
	[Fact]
	public void PredicateBuilder_PropertyInRange_ShouldMatch()
	{
		// 回归测试：修复前判空发生在 MakeGenericMethod 之后，空引用检查失效
		var predicate = PredicateBuilder.PropertyInRange<Item, int>(nameof(Item.Id), 2, 4, 6).Compile();

		Assert.True(predicate(new Item { Id = 4 }));
		Assert.False(predicate(new Item { Id = 5 }));
	}

	/// <summary>
	/// NotContains 操作应对包含判断取反，属性值不包含指定文本时匹配。
	/// </summary>
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

	/// <summary>
	/// Is 操作应判断属性值是否为空引用。
	/// </summary>
	[Fact]
	public void Expression_Operation_Is_ShouldCheckNull()
	{
		var parameter = Expression.Parameter(typeof(Item), "t");
		var expression = parameter.Property(nameof(Item.Name))
		                          .Operation(QueryOperator.Is, null!)
		                          .ToLambda<Func<Item, bool>>(parameter)
		                          .Compile();

		Assert.True(expression(new Item { Id = 1, Name = null! }));
		Assert.False(expression(new Item { Id = 2, Name = "n2" }));
	}

	/// <summary>
	/// 空谓词构建器应返回恒真谓词，避免下游组合时出现空引用。
	/// </summary>
	[Fact]
	public void PredicateExpressionBuilder_Empty_ShouldReturnTruePredicate()
	{
		// 回归测试：修复前空构建器返回 null，下游组合时触发空引用
		var builder = new PredicateExpressionBuilder<Item>();

		var predicate = builder.ToLambda().Compile();

		Assert.True(predicate(new Item { Id = 1 }));
	}

	/// <summary>
	/// 按可空属性值过滤查询时应仅返回命中该值的元素。
	/// </summary>
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
