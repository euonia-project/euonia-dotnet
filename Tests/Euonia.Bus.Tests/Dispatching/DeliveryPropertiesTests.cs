using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对投递属性（目标队列、优先级）从发送选项传递到消息元数据的测试。
/// </summary>
/// <remarks>
/// <see cref="ExtendableOptions.Queue"/> 与 <see cref="ExtendableOptions.Priority"/> 此前
/// 只被写入、从未被任何传输器读取。现在它们经 <see cref="MessageProperties"/> 定义的元数据键
/// 传给传输器，因此这里验证的是发送侧的传递契约。
/// </remarks>
public class DeliveryPropertiesTests
{
	[Fact]
	public async Task PublishAsync_WithQueueAndPriority_WritesThemIntoMetadata()
	{
		var received = new List<IMessageEnvelope>();
		await using var provider = BuildProvider(received);
		var bus = provider.GetRequiredService<IBus>();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/meta" },
			new PublishOptions { Channel = "test.events", MessageId = "meta-1", Queue = "orders.queue", Priority = 7 },
			null,
			TestContext.Current.CancellationToken);

		var envelope = Assert.Single(received);
		Assert.Equal("orders.queue", envelope.GetQueue());
		Assert.Equal(7, envelope.GetPriority());
	}

	[Fact]
	public async Task PublishAsync_WithoutTheseOptions_LeavesMetadataUnset()
	{
		var received = new List<IMessageEnvelope>();
		await using var provider = BuildProvider(received);
		var bus = provider.GetRequiredService<IBus>();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/meta" },
			new PublishOptions { Channel = "test.events", MessageId = "meta-2" },
			null,
			TestContext.Current.CancellationToken);

		var envelope = Assert.Single(received);

		// 未设置时不应写入键，否则会用空值覆盖用户在元数据里自行放置的同名键。
		Assert.False(envelope.Metadata.ContainsKey(MessageProperties.QueueKey));
		Assert.False(envelope.Metadata.ContainsKey(MessageProperties.PriorityKey));
		Assert.Null(envelope.GetQueue());
		Assert.Null(envelope.GetPriority());
	}

	/// <summary>
	/// 优先级为 0 或负数表示「不设置」，不应写入元数据。
	/// </summary>
	[Fact]
	public async Task PublishAsync_WithNonPositivePriority_DoesNotWritePriority()
	{
		var received = new List<IMessageEnvelope>();
		await using var provider = BuildProvider(received);
		var bus = provider.GetRequiredService<IBus>();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/meta" },
			new PublishOptions { Channel = "test.events", MessageId = "meta-3", Priority = 0 },
			null,
			TestContext.Current.CancellationToken);

		var envelope = Assert.Single(received);
		Assert.False(envelope.Metadata.ContainsKey(MessageProperties.PriorityKey));
	}

	/// <summary>
	/// 显式设置的投递选项应优先于 <see cref="ExtendableOptions.MetadataSetter"/> 写入的同名值。
	/// </summary>
	[Fact]
	public async Task PublishAsync_ExplicitOptions_OverrideMetadataSetter()
	{
		var received = new List<IMessageEnvelope>();
		await using var provider = BuildProvider(received);
		var bus = provider.GetRequiredService<IBus>();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/meta" },
			new PublishOptions
			{
				Channel = "test.events",
				MessageId = "meta-4",
				Queue = "explicit.queue",
				MetadataSetter = metadata => metadata[MessageProperties.QueueKey] = "from-metadata",
			},
			null,
			TestContext.Current.CancellationToken);

		var envelope = Assert.Single(received);
		Assert.Equal("explicit.queue", envelope.GetQueue());
	}

	private static ServiceProvider BuildProvider(List<IMessageEnvelope> received)
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
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new CapturingTransporter(received));

		var provider = services.BuildServiceProvider();
		provider.GetRequiredService<IConfigurator>().SetConvention(builder => builder.Add<DefaultMessageConvention>());
		return provider;
	}

	/// <summary>
	/// 记录收到的信封，供断言投递属性。
	/// </summary>
	private sealed class CapturingTransporter(List<IMessageEnvelope> received) : ITransporter
	{
		public string Name => "test";

		public event EventHandler<MessageDeliveredEventArgs> Delivered
		{
			add { }
			remove { }
		}

		public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		{
			received.Add(message);
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
