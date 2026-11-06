using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 执行器构建器复用护栏：同一个 <see cref="ActuatorBuilder{TTarget}"/> 连续执行多次时，
/// 第一次执行不得把管道里已注册的行为（DI 注入的 <see cref="IActuatorBehavior{TTarget}"/> 与
/// <c>ActuatorBuilder&lt;TTarget&gt;.Behavior(Action&lt;IPipeline&lt;TTarget, TTarget&gt;&gt;)</c> 追加的组件）消耗掉。
/// </summary>
public class ActuatorBuilderReuseTests
{
	private readonly IActuator _actuator;

	/// <summary>
	/// 初始化测试实例，并注入待复用的执行器。
	/// </summary>
	/// <param name="actuator">用于创建执行器构建器的执行器。</param>
	public ActuatorBuilderReuseTests(IActuator actuator)
	{
		_actuator = actuator;
	}

	/// <summary>
	/// 同一个构建器连续执行两次时，其注册的行为应在每次执行时都被调用。
	/// </summary>
	[Fact]
	public async Task Reusing_One_Builder_Should_Run_Its_Registered_Behavior_On_Every_Execution()
	{
		var builder = _actuator.For<ActuatorTestCommand>();
		var invocations = 0;

		builder.Behavior(pipeline =>
		{
			pipeline.Use((request, next) =>
			{
				invocations++;
				return next(request);
			});
		});

		await builder.Execute().ExecuteAsync(TestContext.Current.CancellationToken);
		await builder.Execute().ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(2, invocations);
	}

	/// <summary>
	/// 同一个构建器连续执行两次时，注册行为的执行顺序应在每次执行中保持一致。
	/// </summary>
	[Fact]
	public async Task Reusing_One_Builder_Should_Keep_The_Same_Behavior_Order_On_Every_Execution()
	{
		var builder = _actuator.For<ActuatorTestCommand>();
		var executed = new List<string>();

		builder.Behavior(pipeline =>
		{
			pipeline.Use((request, next) => { executed.Add("First"); return next(request); });
			pipeline.Use((request, next) => { executed.Add("Second"); return next(request); });
		});

		await builder.Execute().ExecuteAsync(TestContext.Current.CancellationToken);
		await builder.Execute().ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(["First", "Second", "First", "Second"], executed);
	}
}
