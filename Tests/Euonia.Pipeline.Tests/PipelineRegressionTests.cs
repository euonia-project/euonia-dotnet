using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Pipeline.Tests;

/// <summary>
/// 管道回归测试：覆盖此前未验证的路径，包括方法级行为（Handle/HandleAsync）解析与同步 <see cref="Pipeline"/> 门面。
/// </summary>
public class PipelineRegressionTests
{
	/// <summary>
	/// 管道回归测试用空请求。
	/// </summary>
	private sealed class Request
	{
	}

	/// <summary>
	/// 记录处理器是否被调用并携带返回值的注入对象。
	/// </summary>
	private sealed class Marker
	{
		public bool Called { get; set; }

		public int Value { get; init; }
	}

	/// <summary>
	/// 方法级多参数处理器，用于验证构造参数与方法参数均可注入。
	/// </summary>
	private sealed class MultiParamHandler
	{
#pragma warning disable IDE0052
		private readonly object _next;
#pragma warning restore IDE0052
		private readonly Marker _marker;

		public MultiParamHandler(object next, Marker marker)
		{
			_next = next;
			_marker = marker;
		}

		public Task<int> HandleAsync(Request request, Marker marker)
		{
			_marker.Called = true;
			return Task.FromResult(_marker.Value);
		}
	}

	/// <summary>
	/// 把自身执行写入日志的委托行为，用于验证与累积委托的执行顺序。
	/// </summary>
	private sealed class RecordingDelegateBehavior : IDelegateBehavior<Request>
	{
		private readonly List<string> _log;

		public RecordingDelegateBehavior(List<string> log)
		{
			_log = log;
		}

		public Task HandleAsync(Request request, Delegate next, CancellationToken cancellationToken)
		{
			_log.Add("Behavior");
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// 以方法级多参数处理器注册的行为可以构建并运行：返回注入对象中的值，且该对象被标记为已调用。
	/// </summary>
	[Fact]
	public async Task TypedPipeline_Use_MethodBasedMultiParamHandler_BuildsAndRuns()
	{
		var provider = new ServiceCollection()
			.AddSingleton(new Marker { Value = 17 })
			.BuildServiceProvider();
		var pipeline = new DefaultPipelineProvider<Request, int>(provider);

		pipeline.Use(typeof(MultiParamHandler));

		var result = await pipeline.RunAsync(new Request());

		Assert.Equal(17, result);
		Assert.True(provider.GetRequiredService<Marker>().Called);
	}

	/// <summary>
	/// 静态 <c>Pipeline.Run</c> 先按顺序执行行为、再执行累积委托，并返回累积委托的结果。
	/// </summary>
	[Fact]
	public void StaticRun_returns_accumulate_result_and_runs_behaviors_in_order()
	{
		var log = new List<string>();
		var behaviors = new[] { new RecordingDelegateBehavior(log) };

		var result = Pipeline.Run(new Request(), request =>
		{
			log.Add("Accumulate");
			return 42;
		}, behaviors);

		Assert.Equal(42, result);
		Assert.Equal(new[] { "Behavior", "Accumulate" }, log);
	}

	/// <summary>
	/// 没有行为时，静态 <c>Pipeline.Run</c> 直接执行累积委托并返回其结果。
	/// </summary>
	[Fact]
	public void StaticRun_without_behaviors_returns_accumulate_result()
	{
		var log = new List<string>();
		var behaviors = Array.Empty<IDelegateBehavior<Request>>();

		var result = Pipeline.Run(new Request(), request =>
		{
			log.Add("Accumulate");
			return 7;
		}, behaviors);

		Assert.Equal(7, result);
		Assert.Equal(new[] { "Accumulate" }, log);
	}

	/// <summary>
	/// 无返回值的静态 <c>Pipeline.Run</c> 同样先执行行为、再执行累积委托。
	/// </summary>
	[Fact]
	public void StaticRun_void_runs_behaviors_then_accumulate()
	{
		var log = new List<string>();
		var behaviors = new[] { new RecordingDelegateBehavior(log) };

		Pipeline.Run(new Request(), request => log.Add("Accumulate"), behaviors);

		Assert.Equal(new[] { "Behavior", "Accumulate" }, log);
	}
}