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
	/// <summary>
	/// 记录管道中已执行组件名称的测试请求。
	/// </summary>
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

	/// <summary>
	/// 声明 <see cref="TaggingBehavior"/> 的请求，用于验证按请求类型声明的行为在每次调用时应用一次。
	/// </summary>
	[PipelineBehavior(typeof(TaggingBehavior))]
	private sealed class AttributedRequest : Request
	{
	}

	/// <summary>
	/// 把自身名称写入请求执行记录的管道行为。
	/// </summary>
	private sealed class TaggingBehavior : IPipelineBehavior<Request, int>
	{
		public Task<int> HandleAsync(Request context, PipelineDelegate<Request, int> next)
		{
			context.Executed.Add(nameof(TaggingBehavior));
			return next(context);
		}
	}

	/// <summary>
	/// <see cref="IPipeline{TRequest, TResponse}.Build"/> 是非破坏性的：连续两次构建都执行同一组件，组件表不被消费。
	/// </summary>
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

	/// <summary>
	/// 带累积委托的 <c>RunAsync</c> 不会把累积委托写回组件表：多次运行后组件表仍为空，后续构建也不含它。
	/// </summary>
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

	/// <summary>
	/// 请求类型上声明的行为在每次调用时都应用一次，不会逐次累积；调用后组件表保持为空。
	/// </summary>
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

	/// <summary>
	/// 注册与构建并发进行时组件表保持一致：全部注册完成后数量恰好等于注册次数。
	/// </summary>
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
