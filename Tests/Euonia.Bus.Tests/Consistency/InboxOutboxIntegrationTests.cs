using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 消息总线接入发件箱/收件箱后的端到端测试。
/// </summary>
/// <remarks>
/// 测试直接构建 <see cref="ServiceProvider"/>（不依赖宿主），因此在解析 <see cref="IHandlerContext"/> 之后
/// 才注册通道处理器，以确保 <see cref="DefaultHandlerContext"/> 能订阅到 <see cref="IConfigurator"/> 的 ChannelRegistered 事件。
/// </remarks>
public class InboxOutboxIntegrationTests
{
	/// <summary>
	/// 验证启用发件箱后发布消息会写入发件箱条目，并将传输状态标记为 <see cref="OutboxTransportStatus.Success"/>。
	/// </summary>
	[Fact]
	public async Task PublishAsync_WithOutboxEnabled_InsertsEntryAndMarksSuccess()
	{
		var delivered = new List<string>();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.Configure<MessageBusOptions>(options =>
		{
			options.DefaultTransporter = "test";
			options.Outbox.Enabled = true;
		});
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new RecordingTransporter(delivered));

		await using var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var bus = provider.GetRequiredService<IBus>();
		var store = provider.GetRequiredService<IOutboxStore>();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/1" },
			new PublishOptions { Channel = "test.events", MessageId = "publish-outbox-1" },
			null, TestContext.Current.CancellationToken);

		Assert.Contains("publish-outbox-1", delivered);

		var entry = store.Get("publish-outbox-1");
		Assert.NotNull(entry);
		Assert.Equal("test.events", entry.Channel);
		Assert.Equal(typeof(TestMessages.OrderPlacedEvent).FullName, entry.MessageType);

		var transport = entry.GetTransport("test");
		Assert.NotNull(transport);
		Assert.Equal(OutboxTransportStatus.Success, transport.Status);
		Assert.Empty(store.GetFailedMessages());
	}

	/// <summary>
	/// 验证默认未启用发件箱时，发布消息不会写入发件箱。
	/// </summary>
	[Fact]
	public async Task PublishAsync_WithOutboxDisabledByDefault_DoesNotInsert()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new RecordingTransporter([]));

		await using var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var bus = provider.GetRequiredService<IBus>();
		var store = provider.GetRequiredService<IOutboxStore>();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/2" },
			new PublishOptions { Channel = "test.events", MessageId = "publish-plain-2" },
			null, TestContext.Current.CancellationToken);

		Assert.Null(store.Get("publish-plain-2"));
	}

	/// <summary>
	/// 验证消息级 <c>UseOutbox</c> 可在全局未启用发件箱时强制写入条目。
	/// </summary>
	[Fact]
	public async Task PublishAsync_WithPerMessageUseOutbox_InsertsEvenWhenGlobalDisabled()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new RecordingTransporter([]));

		await using var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var bus = provider.GetRequiredService<IBus>();
		var store = provider.GetRequiredService<IOutboxStore>();

		await bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/3" },
			new PublishOptions { Channel = "test.events", MessageId = "publish-override-3", UseOutbox = true },
			null, TestContext.Current.CancellationToken);

		Assert.NotNull(store.Get("publish-override-3"));
	}

	/// <summary>
	/// 验证启用收件箱后重复投递的多播消息只执行一次处理器，并将处理状态标记为 <see cref="InboxHandlerStatus.Success"/>。
	/// </summary>
	[Fact]
	public async Task HandleAsync_Multicast_WithInboxEnabled_DeduplicatesRedeliveryAndMarksSuccess()
	{
		var handled = 0;
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.Configure<MessageBusOptions>(options => options.Inbox.Enabled = true);
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddSingleton<IInboxStore, InMemoryInboxStore>();

		await using var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var handlerContext = provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel<TestMessages.OrderPlacedEvent>("test.events", (message, context) =>
		{
			handled++;
			return Task.CompletedTask;
		});

		await handlerContext.HandleAsync("test.events", new TestMessages.OrderPlacedEvent { OrderId = "orders/1" }, new MessageContext { MessageId = "inbox-dedup-1" }, TestContext.Current.CancellationToken);
		await handlerContext.HandleAsync("test.events", new TestMessages.OrderPlacedEvent { OrderId = "orders/1" }, new MessageContext { MessageId = "inbox-dedup-1" }, TestContext.Current.CancellationToken);

		Assert.Equal(1, handled);

		var store = provider.GetRequiredService<IInboxStore>();
		var entry = store.Get("inbox-dedup-1");
		Assert.NotNull(entry);
		var handler = Assert.Single(entry.Handlers);
		Assert.Equal(InboxHandlerStatus.Success, handler.Status);
		Assert.Empty(store.GetFailedMessages());
	}

	/// <summary>
	/// 验证未启用收件箱时，重复投递的多播消息会被重复执行。
	/// </summary>
	[Fact]
	public async Task HandleAsync_Multicast_WithInboxDisabled_DoesNotDeduplicate()
	{
		var handled = 0;
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();

		await using var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var handlerContext = provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel<TestMessages.OrderPlacedEvent>("test.events", (message, context) =>
		{
			handled++;
			return Task.CompletedTask;
		});

		await handlerContext.HandleAsync("test.events", new TestMessages.OrderPlacedEvent { OrderId = "orders/2" }, new MessageContext { MessageId = "inbox-nodup-1" }, TestContext.Current.CancellationToken);
		await handlerContext.HandleAsync("test.events", new TestMessages.OrderPlacedEvent { OrderId = "orders/2" }, new MessageContext { MessageId = "inbox-nodup-1" }, TestContext.Current.CancellationToken);

		Assert.Equal(2, handled);
	}

	/// <summary>
	/// 验证启用发件箱但未注册存储时，发布消息抛出 <see cref="MessagePersistentException"/>。
	/// </summary>
	[Fact]
	public async Task PublishAsync_WithOutboxEnabled_WithoutStore_ThrowsMessagePersistentException()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.Configure<MessageBusOptions>(options =>
		{
			options.DefaultTransporter = "test";
			options.Outbox.Enabled = true;
		});
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new RecordingTransporter([]));

		await using var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var bus = provider.GetRequiredService<IBus>();

		await Assert.ThrowsAsync<MessagePersistentException>(() => bus.PublishAsync(
			new TestMessages.OrderPlacedEvent { OrderId = "orders/missing-store" },
			new PublishOptions { Channel = "test.events", MessageId = "publish-nostore-1" },
			null, TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// 验证启用收件箱但未注册存储时，处理消息抛出 <see cref="MessagePersistentException"/>。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithInboxEnabled_WithoutStore_ThrowsMessagePersistentException()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.Configure<MessageBusOptions>(options => options.Inbox.Enabled = true);
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();

		await using var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		var handlerContext = provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel<TestMessages.OrderPlacedEvent>("test.events", (message, context) => Task.CompletedTask);

		await Assert.ThrowsAsync<MessagePersistentException>(() => handlerContext.HandleAsync(
			"test.events",
			new TestMessages.OrderPlacedEvent { OrderId = "orders/missing-store" },
			new MessageContext { MessageId = "inbox-nostore-1" },
			TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// 记录已投递消息标识符的测试传输器。
	/// </summary>
	private sealed class RecordingTransporter : ITransporter
	{
		private readonly List<string> _delivered;

		public RecordingTransporter(List<string> delivered)
		{
			_delivered = delivered;
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
			throw new NotImplementedException();
		}

		public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}