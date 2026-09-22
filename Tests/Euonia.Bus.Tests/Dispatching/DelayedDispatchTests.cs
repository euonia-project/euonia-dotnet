using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="ExtendableOptions.Delay"/> 延迟分发的测试。
/// </summary>
/// <remarks>
/// 该选项此前只被写入、从未被任何传输器读取——文档描述了功能但实现空转。
/// 现在延迟作用于**分发之前**，与传输器无关，对所有内置传输一致生效。
/// </remarks>
public class DelayedDispatchTests
{
	[Fact]
	public async Task PublishAsync_WithDelay_DeliversOnlyAfterDelayElapsed()
	{
		var delivered = new List<string>();
		await using var provider = BuildProvider(delivered);
		var bus = provider.GetRequiredService<IBus>();

		const long delay = 400;
		var stopwatch = Stopwatch.StartNew();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/delayed" },
			new PublishOptions { Channel = "test.events", MessageId = "delayed-1", Delay = delay },
			null,
			TestContext.Current.CancellationToken);

		stopwatch.Stop();

		// PublishAsync 应等待延迟结束；断言放宽到 80% 以避免时钟抖动造成的误判。
		Assert.True(stopwatch.ElapsedMilliseconds >= delay * 0.8, $"expected to wait at least {delay * 0.8} ms, waited {stopwatch.ElapsedMilliseconds} ms");
		Assert.Contains("delayed-1", delivered);
	}

	[Fact]
	public async Task PublishAsync_WithoutDelay_DeliversImmediately()
	{
		var delivered = new List<string>();
		await using var provider = BuildProvider(delivered);
		var bus = provider.GetRequiredService<IBus>();

		var stopwatch = Stopwatch.StartNew();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/immediate" },
			new PublishOptions { Channel = "test.events", MessageId = "immediate-1" },
			null,
			TestContext.Current.CancellationToken);

		stopwatch.Stop();

		Assert.Contains("immediate-1", delivered);
		Assert.True(stopwatch.ElapsedMilliseconds < 500, $"expected no delay, took {stopwatch.ElapsedMilliseconds} ms");
	}

	/// <summary>
	/// 延迟必须可被取消令牌中断，否则关闭流程会被长时间挂住。
	/// </summary>
	[Fact]
	public async Task PublishAsync_WithDelay_CancellationInterruptsWait()
	{
		var delivered = new List<string>();
		await using var provider = BuildProvider(delivered);
		var bus = provider.GetRequiredService<IBus>();

		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/cancelled" },
			new PublishOptions { Channel = "test.events", MessageId = "cancelled-1", Delay = 30_000 },
			null,
			cts.Token));

		Assert.DoesNotContain("cancelled-1", delivered);
	}

	/// <summary>
	/// 超出 <see cref="Task.Delay(TimeSpan, CancellationToken)"/> 支持范围时应给出清晰错误。
	/// </summary>
	[Fact]
	public async Task PublishAsync_WithOutOfRangeDelay_ThrowsArgumentOutOfRange()
	{
		var delivered = new List<string>();
		await using var provider = BuildProvider(delivered);
		var bus = provider.GetRequiredService<IBus>();

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/too-long" },
			new PublishOptions { Channel = "test.events", MessageId = "too-long-1", Delay = long.MaxValue },
			null,
			TestContext.Current.CancellationToken));
	}

	private static ServiceProvider BuildProvider(List<string> delivered)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new RecordingTransporter(delivered));

		var provider = services.BuildServiceProvider();
		provider.GetRequiredService<IConfigurator>().SetConvention(builder => builder.Add<DefaultMessageConvention>());
		return provider;
	}

	private sealed class RecordingTransporter : ITransporter
	{
		private readonly List<string> _delivered;

		public RecordingTransporter(IEnumerable<string> delivered)
		{
			_delivered = delivered as List<string> ?? delivered.ToList();
		}

		public string Name => "test";

		public event EventHandler<MessageDeliveredEventArgs> Delivered
		{
			add { }
			remove { }
		}

		public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			_delivered.Add(message.MessageId);
			return Task.CompletedTask;
		}

		public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}
	}
}
