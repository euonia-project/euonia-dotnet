using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证读取执行器：调用工厂读取方法、允许 Handle 处理，但不触发保存。
/// </summary>
public class FetchActuatorTests
{
	/// <summary>
	/// 验证未提供 Handle 时，读取执行器仍会加载并返回目标对象。
	/// </summary>
	[Fact]
	public async Task Fetch_WithoutHandle_ShouldStillLoad()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleCleanEditable>()
		                           .Fetch("repo-2")
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal("repo-2", result.Id);
		Assert.Equal("initial", result.Name);
	}

	/// <summary>
	/// 验证读取执行器加载对象后会应用 Handle 修改，但不触发任何保存操作。
	/// </summary>
	[Fact]
	public async Task Fetch_ShouldLoadAndHandleWithoutSaving()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleCleanEditable>()
		                           .Fetch("repo-1")
		                           .Handle(item => item.Name = "renamed")
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal("repo-1", result.Id);
		Assert.Equal("renamed", result.Name);
		Assert.Equal(0, result.UpdateCount);
		Assert.Equal(0, result.DeleteCount);
	}
}
