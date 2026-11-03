using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 空管道快路径不得绕过目标类型上 <see cref="PipelineBehaviorAttribute"/> 声明的行为：
/// 行为按请求类型在 RunAsync 执行时解析，快路径直接「后续处理 → 终结」会静默丢掉它们。
/// </summary>
public class PipelineBehaviorFastPathTests
{
	[Fact]
	public async Task DeclaredPipelineBehavior_ShouldRunOnFastPath()
	{
		TrackingBehavior.Ran = 0;

		using var scope = RuleTestHarness.CreateScope(
			out _,
			services => services.AddTransient<TrackingBehavior>());

		var actuator = scope.ServiceProvider.GetRequiredService<IActuator>();

		await actuator.For<AttributedTarget>()
		              .Fetch("fast")
		              .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(1, TrackingBehavior.Ran);
	}

	[Fact]
	public async Task PlainTarget_ShouldStillBypassPipeline()
	{
		TrackingBehavior.Ran = 0;

		using var scope = RuleTestHarness.CreateScope(
			out _,
			services => services.AddTransient<TrackingBehavior>());

		var actuator = scope.ServiceProvider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleCleanEditable>()
		                           .Fetch("plain")
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal("plain", result.Id);
		Assert.Equal(0, TrackingBehavior.Ran);
	}
}

/// <summary>
/// 声明了管道行为的目标对象。
/// </summary>
[PipelineBehavior(typeof(TrackingBehavior))]
public class AttributedTarget : BusinessObject<AttributedTarget>
{
	public static readonly PropertyInfo<string> IdProperty = RegisterProperty(p => p.Id);

	public string Id
	{
		get => GetProperty(IdProperty);
		set => SetProperty(IdProperty, value);
	}

	[FactoryFetch]
	private void Fetch(string id)
	{
		Id = id;
		AcceptChanges();
	}
}

/// <summary>
/// 统计执行次数的管道行为。
/// </summary>
public class TrackingBehavior : IPipelineBehavior<AttributedTarget, AttributedTarget>
{
	/// <summary>行为执行次数。</summary>
	public static int Ran;

	public Task<AttributedTarget> HandleAsync(AttributedTarget context, PipelineDelegate<AttributedTarget, AttributedTarget> next)
	{
		Interlocked.Increment(ref Ran);
		return next(context);
	}
}
