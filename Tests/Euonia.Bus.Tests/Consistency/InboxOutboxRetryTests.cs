using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="OutboxDispatcher"/> 与 <see cref="InboxDispatcher"/> 后台重试逻辑的测试。
/// </summary>
public class InboxOutboxRetryTests
{
	private static ServiceProvider BuildOutboxProvider(IEnumerable<string> delivered)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "test");
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
		services.AddKeyedSingleton<ITransporter>("test", (_, _) => new RecordingTransporter(delivered));
		return services.BuildServiceProvider();
	}

	[Fact]
	public async Task OutboxDispatcher_RetryAllAsync_RedeliversFailedMessageAndMarksSuccess()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var accessor = provider.GetRequiredService<IServiceAccessor>();
		IOutboxStore store = provider.GetRequiredService<IOutboxStore>();

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/retry" }, "test.events", "outbox-retry-1");
		store.Insert(envelope, ["test"]);
		var failed = store.Get("outbox-retry-1").GetTransport("test");
		failed.MarkAsFailed("first attempt failed");

		var dispatcher = new OutboxDispatcher(accessor, store, new OutboxOptions());
		await dispatcher.RetryAllAsync();

		Assert.Contains("outbox-retry-1", delivered);
		var transport = store.Get("outbox-retry-1").GetTransport("test");
		Assert.Equal(OutboxTransportStatus.Success, transport.Status);
		Assert.Equal(1, transport.RetryAttempts);
		Assert.Empty(store.GetFailedMessages());
	}

	[Fact]
	public async Task InboxDispatcher_RetryAllAsync_ReexecutesFailedHandlerAndMarksSuccess()
	{
		var handled = 0;
		var services = new ServiceCollection();
		services.AddLogging();
		await using var provider = services.BuildServiceProvider();

		var container = new ConcurrentDictionary<string, List<HandlerRegistration>>
		{
			["test.events"] =
			[
				new HandlerRegistration("MyHandler", _ => (message, context, token) =>
				{
					handled++;
					return Task.FromResult<object>(null);
				})
			]
		};

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/retry" }, "test.events", "inbox-retry-1");
		IInboxStore store = new InMemoryInboxStore();
		store.Insert("test.events", envelope, ["MyHandler"]);
		var failed = store.Get("inbox-retry-1").GetHandler("MyHandler");
		failed.MarkAsFailed("first attempt failed");

		var dispatcher = new InboxDispatcher(provider, store, new InboxOptions { Enabled = true }, container);
		await dispatcher.RetryAllAsync();

		Assert.Equal(1, handled);
		var handler = store.Get("inbox-retry-1").GetHandler("MyHandler");
		Assert.Equal(InboxHandlerStatus.Success, handler.Status);
		Assert.Equal(1, handler.RetryAttempts);
		Assert.Empty(store.GetFailedMessages());
	}

	[Fact]
	public async Task OutboxDispatcher_RetryAllAsync_RespectsMaxRetryAttempts()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var accessor = provider.GetRequiredService<IServiceAccessor>();

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/retry" }, "test.events", "outbox-retry-2");
		IOutboxStore store = new InMemoryOutboxStore();
		store.Insert(envelope, ["test"]);
		var failed = store.Get("outbox-retry-2").GetTransport("test");
		failed.MarkAsFailed("first attempt failed");
		failed.MarkAsFailed("second attempt failed");

		var dispatcher = new OutboxDispatcher(accessor, store, new OutboxOptions { MaxRetryAttempts = 1 });
		await dispatcher.RetryAllAsync();

		Assert.DoesNotContain("outbox-retry-2", delivered);
		var transport = store.Get("outbox-retry-2").GetTransport("test");
		Assert.Equal(OutboxTransportStatus.Failed, transport.Status);
		Assert.Equal(2, transport.RetryAttempts);
	}

	[Fact]
	public async Task OutboxDispatcher_RetryAllAsync_DoesNotRedeliverPendingInFlightMessages()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var accessor = provider.GetRequiredService<IServiceAccessor>();

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/retry" }, "test.events", "outbox-inflight-1");
		IOutboxStore store = new InMemoryOutboxStore();
		store.Insert(envelope, ["test"]);

		var dispatcher = new OutboxDispatcher(accessor, store, new OutboxOptions());
		await dispatcher.RetryAllAsync();

		Assert.Empty(delivered);
		var transport = store.Get("outbox-inflight-1").GetTransport("test");
		Assert.Equal(OutboxTransportStatus.Pending, transport.Status);
		Assert.Empty(store.GetFailedMessages());
	}

	[Fact]
	public async Task InboxDispatcher_RetryAllAsync_DoesNotReexecutePendingInFlightHandlers()
	{
		var handled = 0;
		var services = new ServiceCollection();
		services.AddLogging();
		await using var provider = services.BuildServiceProvider();

		var container = new ConcurrentDictionary<string, List<HandlerRegistration>>
		{
			["test.events"] =
			[
				new HandlerRegistration("MyHandler", _ => (message, context, token) =>
				{
					handled++;
					return Task.FromResult<object>(null);
				})
			]
		};

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/retry" }, "test.events", "inbox-inflight-1");
		IInboxStore store = new InMemoryInboxStore();
		store.Insert("test.events", envelope, ["MyHandler"]);

		var dispatcher = new InboxDispatcher(provider, store, new InboxOptions { Enabled = true }, container);
		await dispatcher.RetryAllAsync();

		Assert.Equal(0, handled);
		Assert.Empty(store.GetFailedMessages());
	}

	/// <summary>
	/// 记录已投递消息标识符的测试传输器。
	/// </summary>
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
			throw new NotImplementedException();
		}

		public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}