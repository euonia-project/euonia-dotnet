namespace Nerosoft.Euonia.Repository.Tests;

/// <summary>
/// 针对 <see cref="RepositoryExtensions"/> 分页行为的回归测试。
/// </summary>
public class RepositoryExtensionsTests
{
	[Fact]
	public void Paginate_OffsetOverflowsInt32_ShouldReturnEmptyInsteadOfThrowing()
	{
		// 回归测试：偏移量曾以 int 相乘，page * size 超过 int.MaxValue 时回绕为负数，
		// Skip(负数) 在 LINQ to Objects 下抛 ArgumentOutOfRangeException。
		var source = Enumerable.Range(1, 10).AsQueryable();

		var page = source.Paginate(page: 50_000, size: 50_000).ToList();

		Assert.Empty(page);
	}

	[Fact]
	public void Paginate_NegativeSize_ShouldReturnEmpty()
	{
		var source = Enumerable.Range(1, 10).AsQueryable();

		var page = source.Paginate(1, -1).ToList();

		Assert.Empty(page);
	}
}
