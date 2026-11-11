using System.Collections.Concurrent;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Core.Tests.Execution;

/// <summary>
/// 并发作用域隔离：AsyncLocal 状态不跨任务泄漏。
/// </summary>
public class ExecutionContextConcurrencyTests
{
	/// <summary>
	/// 并行任务各自安装租户上下文，互不覆盖。
	/// </summary>
	[Fact]
	public async Task ParallelScopes_ShouldIsolateTenants()
	{
		const int count = 64;
		var observed = new ConcurrentBag<string?>();

		await Task.WhenAll(Enumerable.Range(0, count).Select(async index =>
		{
			using var scope = AmbientExecutionContext.Push(values => values.TenantId = $"t{index}");
			await Task.Yield();
			observed.Add(AmbientExecutionContext.Current?.TenantId);
		}));

		Assert.Equal(count, observed.Distinct().Count());
		Assert.Null(AmbientExecutionContext.Current);
	}
}
