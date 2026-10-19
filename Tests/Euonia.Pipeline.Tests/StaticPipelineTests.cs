namespace Nerosoft.Euonia.Pipeline.Tests;

/// <summary>
/// 针对 <see cref="Pipeline"/> 静态门面的回归测试。
/// </summary>
public class StaticPipelineTests
{
	[Fact]
	public async Task RunAsync_ShouldExecuteBehaviorsInEnumerationOrder()
	{
		// 回归测试：Aggregate 使第一个行为成为最内层，实际执行顺序与文档相反。
		var log = new List<string>();
		IPipelineBehavior<int, string>[] behaviors = [new LoggingBehavior(log, "A"), new LoggingBehavior(log, "B")];

		await Pipeline.RunAsync<int, string>(1, _ =>
		{
			log.Add("Handler");
			return Task.FromResult("ok");
		}, behaviors);

		Assert.Equal(["A", "B", "Handler"], log);
	}

	[Fact]
	public void Run_ShouldInvokeDownstreamOnlyOnce()
	{
		// 回归测试：同步门面在 behavior.HandleAsync 之后又调用了一次下游委托。
		var invocations = 0;
		IDelegateBehavior<int>[] behaviors = [new PassThroughBehavior(), new PassThroughBehavior()];

		var result = Pipeline.Run<int, int>(1, _ =>
		{
			invocations++;
			return 42;
		}, behaviors);

		Assert.Equal(42, result);
		Assert.Equal(1, invocations);
	}

	private sealed class LoggingBehavior(List<string> log, string name) : IPipelineBehavior<int, string>
	{
		public Task<string> HandleAsync(int context, PipelineDelegate<int, string> next)
		{
			log.Add(name);
			return next(context);
		}
	}

	private sealed class PassThroughBehavior : IDelegateBehavior<int>
	{
		public Task HandleAsync(int request, Delegate next, CancellationToken cancellationToken)
		{
			next.DynamicInvoke(request);
			return Task.CompletedTask;
		}
	}
}
