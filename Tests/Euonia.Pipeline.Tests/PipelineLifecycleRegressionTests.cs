using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Pipeline.Tests;

/// <summary>
/// 管道组件生命周期回归护栏：
/// <list type="bullet">
/// <item>构建（<see cref="IPipeline{TRequest, TResponse}.Build"/>）是<strong>非破坏性</strong>的——不会清空已注册组件；</item>
/// <item>累积委托与按请求类型解析的行为属于「这一次运行」的输入，不得写回共享组件表；</item>
/// <item>注册与构建可来自不同线程，组件表必须在锁内读写。</item>
/// </list>
/// </summary>
public class PipelineLifecycleRegressionTests
{
	private class Request
	{
		public List<string> Executed { get; } = [];
	}

	/// <summary>
	/// 仅记录执行顺序的测试管道。
	/// </summary>
	private sealed class TestPipeline : PipelineBase<Request, int>
	{
		protected override PipelineDelegate<Request, int> GetNext(PipelineDelegate<Request, int> next, Type type, params object[] constructorArguments)
		{
			return next;
		}
	}

	[PipelineBehavior(typeof(TaggingBehavior))]
	private sealed class AttributedRequest : Request
	{
	}

	private sealed class TaggingBehavior : IPipelineBehavior<Request, int>
	{
		public Task<int> HandleAsync(Request context, PipelineDelegate<Request, int> next)
		{
			context.Executed.Add(nameof(TaggingBehavior));
			return next(context);
		}
	}

	[Fact]
	public async Task Build_Should_Not_Consume_Registered_Components()
	{
		var pipeline = new TestPipeline();
		pipeline.Use(next => context => { context.Executed.Add("A"); return next(context); });

		var first = new Request();
		await pipeline.Build()(first);
		Assert.Equal(["A"], first.Executed);

		var second = new Request();
		await pipeline.Build()(second);
		Assert.Equal(["A"], second.Executed);

		Assert.Single(pipeline.Components);
	}

	[Fact]
	public async Task RunAsync_With_Accumulate_Should_Not_Leak_Accumulate_Into_Registered_Components()
	{
		var pipeline = new TestPipeline();

		Task<int> Accumulate(Request request)
		{
			request.Executed.Add("Accumulate");
			return Task.FromResult(1);
		}

		await pipeline.RunAsync(new Request(), Accumulate);
		await pipeline.RunAsync(new Request(), Accumulate);

		Assert.Empty(pipeline.Components);

		var later = new Request();
		await pipeline.Build()(later);
		Assert.Empty(later.Executed);
	}

	[Fact]
	public async Task RunAsync_Should_Apply_Request_Behaviors_On_Every_Invocation_Without_Piling_Up()
	{
		var provider = new ServiceCollection().BuildServiceProvider();
		IPipeline<Request, int> pipeline = new DefaultPipelineProvider<Request, int>(provider);

		for (var i = 0; i < 3; i++)
		{
			var request = new AttributedRequest();
			await pipeline.RunAsync(request);

			Assert.Equal([nameof(TaggingBehavior)], request.Executed);
			Assert.Empty(pipeline.Components);
		}
	}

	[Fact]
	public async Task Components_Should_Remain_Consistent_While_Registration_Races_With_Building()
	{
		var pipeline = new TestPipeline();
		const int RegisterCount = 400;

		var registering = Task.Run(() =>
		{
			for (var i = 0; i < RegisterCount; i++)
			{
				pipeline.Use(next => context => next(context));
			}
		}, TestContext.Current.CancellationToken);

		var building = Task.Run(async () =>
		{
			for (var i = 0; i < 400; i++)
			{
				var @delegate = pipeline.Build();
				await @delegate(new Request());
				_ = pipeline.Components.Count;
			}
		}, TestContext.Current.CancellationToken);

		await Task.WhenAll(registering, building);

		Assert.Equal(RegisterCount, pipeline.Components.Count);
	}
}
