using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 执行器构建器复用护栏：同一个 <see cref="ActuatorBuilder{TTarget}"/> 连续执行多次时，
/// 第一次执行不得把管道里已注册的行为（DI 注入的 <see cref="IActuatorBehavior{TTarget}"/> 与
/// <see cref="ActuatorBuilder{TTarget}.Behavior(Action{IPipeline{TTarget,TTarget}})"/> 追加的组件）消耗掉。
/// </summary>
public class ActuatorBuilderReuseTests
{
	private readonly IActuator _actuator;

	public ActuatorBuilderReuseTests(IActuator actuator)
	{
		_actuator = actuator;
	}

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
