using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Pipeline.Tests;

/// <summary>
/// 管道回归测试：覆盖此前未验证的路径，包括方法级行为（Handle/HandleAsync）解析与同步 <see cref="Pipeline"/> 门面。
/// </summary>
public class PipelineRegressionTests
{
	private sealed class Request
	{
	}

	private sealed class Marker
	{
		public bool Called { get; set; }

		public int Value { get; init; }
	}

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

	[Fact]
	public async Task UntypedPipeline_Use_MethodBasedMultiParamHandler_WithTypedFirstParameter_BuildsAndRuns()
	{
		var provider = new ServiceCollection()
			.AddSingleton(new Marker { Value = 42 })
			.BuildServiceProvider();
		var pipeline = new DefaultPipelineProvider(provider);
		var context = new Request();

		pipeline.Use(typeof(MultiParamHandler));

		var @delegate = pipeline.Build();
		await @delegate(context);

		Assert.True(provider.GetRequiredService<Marker>().Called);
	}

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

	[Fact]
	public void StaticRun_void_runs_behaviors_then_accumulate()
	{
		var log = new List<string>();
		var behaviors = new[] { new RecordingDelegateBehavior(log) };

		Pipeline.Run(new Request(), request => log.Add("Accumulate"), behaviors);

		Assert.Equal(new[] { "Behavior", "Accumulate" }, log);
	}
}