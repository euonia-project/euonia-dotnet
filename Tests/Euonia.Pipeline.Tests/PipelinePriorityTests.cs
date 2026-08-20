using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Pipeline.Tests;

/// <summary>
/// 管道组件优先级行为测试：数字高的靠后执行，同优先级保持注册顺序。
/// </summary>
public class PipelinePriorityTests
{
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

	private sealed class FirstBehavior : IPipelineBehavior<Request, int>
	{
		public Task<int> HandleAsync(Request context, PipelineDelegate<Request, int> next)
		{
			context.Executed.Add(nameof(FirstBehavior));
			return next(context);
		}
	}

	private sealed class SecondBehavior : IPipelineBehavior<Request, int>
	{
		public Task<int> HandleAsync(Request context, PipelineDelegate<Request, int> next)
		{
			context.Executed.Add(nameof(SecondBehavior));
			return next(context);
		}
	}

	[PipelineBehavior(typeof(FirstBehavior), 20)]
	[PipelineBehavior(typeof(SecondBehavior), 5)]
	private sealed class PrioritizedRequest
	{
	}

	[PipelineBehavior(typeof(FirstBehavior))]
	private sealed class AheadRequest
	{
	}

	[PipelineBehavior(typeof(FirstBehavior))]
	private sealed class AttributedRequest : Request
	{
	}

	[PipelineBehavior(typeof(object), 100)]
	private sealed class AttributedBehavior : IPipelineBehavior<Request, int>
	{
		public Task<int> HandleAsync(Request context, PipelineDelegate<Request, int> next)
		{
			context.Executed.Add(nameof(AttributedBehavior));
			return next(context);
		}
	}

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
