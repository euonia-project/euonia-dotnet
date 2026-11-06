using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Pipeline.Tests;

/// <summary>
/// 管道组件优先级行为测试：数字高的靠后执行，同优先级保持注册顺序。
/// </summary>
public class PipelinePriorityTests
{
	/// <summary>
	/// 记录执行顺序的测试请求。
	/// </summary>
	private class Request
	{
		public List<string> Executed { get; } = new();
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
	/// 把自身名称写入执行记录的行为，用于验证不同优先级与提前放置的排序。
	/// </summary>
	private sealed class FirstBehavior : IPipelineBehavior<Request, int>
	{
		public Task<int> HandleAsync(Request context, PipelineDelegate<Request, int> next)
		{
			context.Executed.Add(nameof(FirstBehavior));
			return next(context);
		}
	}

	/// <summary>
	/// 把自身名称写入执行记录的行为，用于验证不同优先级之间的排序。
	/// </summary>
	private sealed class SecondBehavior : IPipelineBehavior<Request, int>
	{
		public Task<int> HandleAsync(Request context, PipelineDelegate<Request, int> next)
		{
			context.Executed.Add(nameof(SecondBehavior));
			return next(context);
		}
	}

	/// <summary>
	/// 同时声明优先级 20 与 5 两个行为的请求，用于验证数字高的靠后执行。
	/// </summary>
	[PipelineBehavior(typeof(FirstBehavior), 20)]
	[PipelineBehavior(typeof(SecondBehavior), 5)]
	private sealed class PrioritizedRequest
	{
	}

	/// <summary>
	/// 声明 <see cref="FirstBehavior"/> 的请求，用于验证 <c>useAheadOfOthers</c> 标志。
	/// </summary>
	[PipelineBehavior(typeof(FirstBehavior))]
	private sealed class AheadRequest
	{
	}

	/// <summary>
	/// 继承 <see cref="Request"/> 并声明 <see cref="FirstBehavior"/> 的请求，
	/// 用于验证带累积委托时先执行请求声明的行为。
	/// </summary>
	[PipelineBehavior(typeof(FirstBehavior))]
	private sealed class AttributedRequest : Request
	{
	}

	/// <summary>
	/// 特性优先级为 100 的行为，用于验证 <c>Use&lt;TBehavior&gt;</c> 在未显式指定优先级时读取特性值。
	/// </summary>
	[PipelineBehavior(typeof(object), 100)]
	private sealed class AttributedBehavior : IPipelineBehavior<Request, int>
	{
		public Task<int> HandleAsync(Request context, PipelineDelegate<Request, int> next)
		{
			context.Executed.Add(nameof(AttributedBehavior));
			return next(context);
		}
	}

	/// <summary>组件按优先级构建：数字高的靠后执行，与注册顺序无关。</summary>
	[Fact]
	public async Task Build_orders_components_by_priority_higher_later()
	{
		var pipeline = new TestPipeline();
		var context = new Request();

		// 先注册但优先级高 → 靠后执行
		pipeline.Use(next => ctx => { ctx.Executed.Add("Late"); return next(ctx); }, 10);
		pipeline.Use(next => ctx => { ctx.Executed.Add("Early"); return next(ctx); }, 0);

		var @delegate = pipeline.Build();
		await @delegate(context);

		Assert.Equal(new[] { "Early", "Late" }, context.Executed);
	}

	/// <summary>
	/// 带累积委托的运行时，优先级大于 0 的组件仍会执行，且先于累积委托；返回值来自累积委托。
	/// </summary>
	[Fact]
	public async Task RunAsync_WithAccumulate_ShouldStillExecutePositivePriorityComponents()
	{
		// 回归测试：累积委托曾被按优先级 0 混入组件表，导致优先级 > 0 的组件被它包住而永不执行。
		var pipeline = new TestPipeline();
		var context = new Request();

		pipeline.Use(next => ctx => { ctx.Executed.Add("Component"); return next(ctx); }, 10);

		var result = await pipeline.RunAsync(context, ctx =>
		{
			ctx.Executed.Add("Accumulate");
			return Task.FromResult(1);
		});

		Assert.Equal(1, result);
		Assert.Equal(new[] { "Component", "Accumulate" }, context.Executed);
	}

	/// <summary>同优先级的组件保持注册顺序执行。</summary>
	[Fact]
	public async Task Build_keeps_registration_order_for_equal_priority()
	{
		var pipeline = new TestPipeline();
		var context = new Request();

		pipeline.Use(next => ctx => { ctx.Executed.Add("First"); return next(ctx); });
		pipeline.Use(next => ctx => { ctx.Executed.Add("Second"); return next(ctx); });

		var @delegate = pipeline.Build();
		await @delegate(context);

		Assert.Equal(new[] { "First", "Second" }, context.Executed);
	}

	/// <summary><c>UseOf</c> 按特性声明的优先级注册行为，与手工注册的组件共同排序。</summary>
	[Fact]
	public async Task UseOf_registers_behaviors_with_attribute_priority()
	{
		var provider = new ServiceCollection().BuildServiceProvider();
		var pipeline = new DefaultPipelineProvider<Request, int>(provider);
		var context = new Request();

		pipeline.Use(next => ctx => { ctx.Executed.Add("Manual"); return next(ctx); }, 10);
		pipeline.UseOf<PrioritizedRequest>();

		var @delegate = pipeline.Build();
		await @delegate(context);

		// SecondBehavior 优先级 5 < Manual 10 < FirstBehavior 20
		Assert.Equal(new[] { "SecondBehavior", "Manual", "FirstBehavior" }, context.Executed);
	}

	/// <summary><c>UseOf</c> 的 <c>useAheadOfOthers</c> 标志把行为放到手工注册组件之前。</summary>
	[Fact]
	public async Task UseOf_with_ahead_flag_places_behaviors_first()
	{
		var provider = new ServiceCollection().BuildServiceProvider();
		var pipeline = new DefaultPipelineProvider<Request, int>(provider);
		var context = new Request();

		pipeline.Use(next => ctx => { ctx.Executed.Add("Manual"); return next(ctx); });
		pipeline.UseOf<AheadRequest>(useAheadOfOthers: true);

		var @delegate = pipeline.Build();
		await @delegate(context);

		Assert.Equal(new[] { "FirstBehavior", "Manual" }, context.Executed);
	}

	/// <summary>未显式指定优先级时，<c>Use</c> 读取行为类型上的 <c>PipelineBehavior</c> 特性优先级。</summary>
	[Fact]
	public async Task Use_TBehavior_reads_priority_from_attribute_when_not_specified()
	{
		var provider = new ServiceCollection().BuildServiceProvider();
		var pipeline = new DefaultPipelineProvider<Request, int>(provider);
		var context = new Request();

		pipeline.Use(next => ctx => { ctx.Executed.Add("Manual"); return next(ctx); });
		pipeline.Use<AttributedBehavior>();

		var @delegate = pipeline.Build();
		await @delegate(context);

		// AttributedBehavior 特性优先级 100，靠后执行
		Assert.Equal(new[] { "Manual", "AttributedBehavior" }, context.Executed);
	}

	/// <summary>带累积委托的运行时先执行请求声明的行为，再执行累积委托并返回其结果。</summary>
	[Fact]
	public async Task RunAsync_with_accumulate_executes_behaviors_before_accumulate()
	{
		var provider = new ServiceCollection().BuildServiceProvider();
		var pipeline = new DefaultPipelineProvider<Request, int>(provider);
		var context = new AttributedRequest();

		var result = await pipeline.RunAsync(context, ctx =>
		{
			ctx.Executed.Add("Accumulate");
			return Task.FromResult(42);
		});

		Assert.Equal(42, result);
		Assert.Equal(new[] { "FirstBehavior", "Accumulate" }, context.Executed);
	}
}
