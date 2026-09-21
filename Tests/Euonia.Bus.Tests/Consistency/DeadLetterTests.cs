using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对死信队列（重试耗尽后的归档与重放）的测试。
/// </summary>
public class DeadLetterTests
{
	[Fact]
	public async Task OutboxDispatcher_WhenRetriesExhausted_MovesRecordToDeadLetters()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var accessor = provider.GetRequiredService<IServiceAccessor>();
		var deadLetters = provider.GetRequiredService<IDeadLetterStore>();

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/dead" }, "test.events", "dead-outbox-1");
		IOutboxStore store = new InMemoryOutboxStore();
		store.Insert(envelope, ["test"]);
		var failed = store.Get("dead-outbox-1").GetTransport("test");
		failed.MarkAsFailed("first attempt failed");
		failed.MarkAsFailed("second attempt failed");

		var dispatcher = new OutboxDispatcher(accessor, store, new OutboxOptions { MaxRetryAttempts = 1 });
		await dispatcher.RetryAllAsync();

		// 不再重投递，且已从失败集合中移除（否则每轮轮询都会重复扫描）。
		Assert.DoesNotContain("dead-outbox-1", delivered);
		Assert.Equal(OutboxTransportStatus.DeadLettered, store.Get("dead-outbox-1").GetTransport("test").Status);
		Assert.Empty(store.GetFailedMessages());

		var entry = deadLetters.Get("dead-outbox-1");
		Assert.NotNull(entry);
		Assert.Equal(DeadLetterSource.Outbox, entry.Source);
		Assert.Equal("test", entry.Target);
		Assert.Equal("test.events", entry.Channel);
		Assert.Equal(2, entry.RetryAttempts);
		Assert.Equal("second attempt failed", entry.Error);
	}

	[Fact]
	public async Task OutboxDispatcher_WhenRetriesExhaustedWithoutDeadLetterStore_StillMarksDeadLettered()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered, registerDeadLetters: false);
		var accessor = provider.GetRequiredService<IServiceAccessor>();

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/dead" }, "test.events", "dead-outbox-2");
		IOutboxStore store = new InMemoryOutboxStore();
		store.Insert(envelope, ["test"]);
		// MaxRetryAttempts = 1 表示允许 1 次重投递，因此需要累计 2 次失败才算耗尽。
		var failed = store.Get("dead-outbox-2").GetTransport("test");
		failed.MarkAsFailed("first attempt failed");
		failed.MarkAsFailed("second attempt failed");

		var dispatcher = new OutboxDispatcher(accessor, store, new OutboxOptions { MaxRetryAttempts = 1 });
		await dispatcher.RetryAllAsync();

		// 未注册死信存储时保持原有的"跳过"语义，但状态仍转为终态，避免被反复扫描。
		Assert.Equal(OutboxTransportStatus.DeadLettered, store.Get("dead-outbox-2").GetTransport("test").Status);
		Assert.Empty(store.GetFailedMessages());
	}

	[Fact]
	public async Task InboxDispatcher_WhenRetriesExhausted_MovesRecordToDeadLetters()
	{
		var handled = 0;
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddInMemoryDeadLetters();
		await using var provider = services.BuildServiceProvider();
		var deadLetters = provider.GetRequiredService<IDeadLetterStore>();

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

		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/dead" }, "test.events", "dead-inbox-1");
		IInboxStore store = new InMemoryInboxStore();
		store.Insert("test.events", envelope, ["MyHandler"]);
		// MaxRetryAttempts = 1 表示允许 1 次重执行，因此需要累计 2 次失败才算耗尽。
		var failed = store.Get("dead-inbox-1").GetHandler("MyHandler");
		failed.MarkAsFailed("first attempt failed");
		failed.MarkAsFailed("second attempt failed");

		var dispatcher = new InboxDispatcher(provider, store, new InboxOptions { Enabled = true, MaxRetryAttempts = 1 }, container);
		await dispatcher.RetryAllAsync();

		Assert.Equal(0, handled);
		Assert.Equal(InboxHandlerStatus.DeadLettered, store.Get("dead-inbox-1").GetHandler("MyHandler").Status);
		Assert.Empty(store.GetFailedMessages());

		var entry = deadLetters.Get("dead-inbox-1");
		Assert.NotNull(entry);
		Assert.Equal(DeadLetterSource.Inbox, entry.Source);
		Assert.Equal("MyHandler", entry.Target);
	}

	[Fact]
	public async Task ReplayAsync_ForOutboxSource_RedeliversAndRemovesEntry()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var service = provider.GetRequiredService<IDeadLetterService>();

		AddDeadLetter(provider, "replay-outbox-1", DeadLetterSource.Outbox, "test");

		Assert.True(await service.ReplayAsync("replay-outbox-1", TestContext.Current.CancellationToken));

		Assert.Contains("replay-outbox-1", delivered);
		Assert.Null(service.Get("replay-outbox-1"));
		Assert.Empty(service.GetAll());
	}

	[Fact]
	public async Task ReplayAsync_ForInboxSource_ReexecutesHandlerAndRemovesEntry()
	{
		var handled = 0;
		await using var provider = BuildInboxProvider(_ => handled++);
		var service = provider.GetRequiredService<IDeadLetterService>();

		AddDeadLetter(provider, "replay-inbox-1", DeadLetterSource.Inbox, "MyHandler");

		Assert.True(await service.ReplayAsync("replay-inbox-1", TestContext.Current.CancellationToken));

		Assert.Equal(1, handled);
		Assert.Null(service.Get("replay-inbox-1"));
	}

	[Fact]
	public async Task ReplayAsync_WhenTransportMissing_KeepsEntryAndRecordsError()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var service = provider.GetRequiredService<IDeadLetterService>();

		AddDeadLetter(provider, "replay-missing-1", DeadLetterSource.Outbox, "not-registered");

		Assert.False(await service.ReplayAsync("replay-missing-1", TestContext.Current.CancellationToken));

		var entry = service.Get("replay-missing-1");
		Assert.NotNull(entry);
		Assert.Contains("not-registered", entry.Error);
	}

	[Fact]
	public async Task ReplayAsync_WithUnknownMessageId_ReturnsFalse()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var service = provider.GetRequiredService<IDeadLetterService>();

		Assert.False(await service.ReplayAsync("does-not-exist", TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Discard_RemovesEntryWithoutReplaying()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var service = provider.GetRequiredService<IDeadLetterService>();

		AddDeadLetter(provider, "discard-1", DeadLetterSource.Outbox, "test");

		Assert.True(service.Discard("discard-1"));
		Assert.Empty(delivered);
		Assert.Null(service.Get("discard-1"));
		Assert.False(service.Discard("discard-1"));
	}

	/// <summary>
	/// 回归测试：<c>GetFailedMessages</c> 返回的是存储快照，对它的修改在持久化实现中不会落库。
	/// 终态必须经存储接口的 <c>MarkAsDeadLettered</c> 持久化，否则记录会每轮被重复扫描。
	/// </summary>
	[Fact]
	public async Task OutboxDispatcher_WithDetachedStore_PersistsDeadLetteredState()
	{
		var delivered = new List<string>();
		await using var provider = BuildOutboxProvider(delivered);
		var accessor = provider.GetRequiredService<IServiceAccessor>();

		// 用接口类型调用：Insert(信封, 传输器名) 是默认接口方法，只能经接口访问。
		IOutboxStore store = new DetachedOutboxStore();
		store.Insert(TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/dead" }, "test.events", "detached-1"), ["test"]);
		var failed = store.Get("detached-1").GetTransport("test");
		failed.MarkAsFailed("first attempt failed");
		failed.MarkAsFailed("second attempt failed");

		var dispatcher = new OutboxDispatcher(accessor, store, new OutboxOptions { MaxRetryAttempts = 1 });
		await dispatcher.RetryAllAsync();

		// 若实现改为直接修改 GetFailedMessages 返回的对象，此处仍会返回该记录（状态未落库）。
		Assert.Empty(store.GetFailedMessages());
		Assert.Equal(OutboxTransportStatus.DeadLettered, store.Get("detached-1").GetTransport("test").Status);
	}

	/// <summary>
	/// 写入一条死信记录，其信封与 <paramref name="messageId"/> 对应。
	/// </summary>
	private static void AddDeadLetter(IServiceProvider provider, string messageId, DeadLetterSource source, string target)
	{
		var envelope = TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/dead" }, "test.events", messageId);
		provider.GetRequiredService<IDeadLetterStore>().Add(new DeadLetterEntry
		{
			MessageId = messageId,
			Channel = "test.events",
			MessageType = typeof(TestMessages.OrderPlacedEvent).FullName,
			Content = envelope,
			Source = source,
			Target = target,
			Error = "boom",
			RetryAttempts = 3,
		});
	}

	private static ServiceProvider BuildOutboxProvider(IEnumerable<string> delivered, bool registerDeadLetters = true)
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

		if (registerDeadLetters)
		{
			services.AddInMemoryDeadLetters();
		}

		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 构造一个收件箱重放场景的容器：包含 <see cref="IHandlerContext"/> 与一个记录调用次数的处理程序。
	/// </summary>
	private static ServiceProvider BuildInboxProvider(Action<object> onHandled)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddEuoniaBus();
		services.AddInMemoryDeadLetters();
		services.AddSingleton<IHandlerContext>(_ => new DelegatingHandlerContext(onHandled));

		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 处理程序上下文替身：调用处理委托并立即回复结果。
	/// </summary>
	private sealed class DelegatingHandlerContext(Action<object> onHandled) : IHandlerContext
	{
		public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed
		{
			add { }
			remove { }
		}

		public Task<object> HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
		{
			onHandled(message);
			return Task.FromResult<object>(message);
		}
	}

	/// <summary>
	/// 模拟持久化存储：<see cref="GetFailedMessages"/> 返回**快照副本**而非内部对象，
	/// 因此只有经存储接口的状态变更方法才能落库。
	/// </summary>
	private sealed class DetachedOutboxStore : IOutboxStore
	{
		private readonly InMemoryOutboxStore _inner = new();

		public bool Insert(OutboxEntry entry)
		{
			return _inner.Insert(entry);
		}

		public OutboxEntry Get(string messageId)
		{
			return _inner.Get(messageId);
		}

		public void MarkAsSuccess(string messageId, string transport)
		{
			_inner.MarkAsSuccess(messageId, transport);
		}

		public void MarkAsFailed(string messageId, string transport, string errorMessage)
		{
			_inner.MarkAsFailed(messageId, transport, errorMessage);
		}

		public void MarkAsDeadLettered(string messageId, string transport, string errorMessage)
		{
			_inner.MarkAsDeadLettered(messageId, transport, errorMessage);
		}

		public IReadOnlyList<OutboxTransport> GetFailedMessages()
		{
			// 返回副本，模拟数据库查询物化出的新对象。
			return
			[
				.. _inner.GetFailedMessages().Select(transport => new OutboxTransport
				{
					MessageId = transport.MessageId,
					Name = transport.Name,
					Status = transport.Status,
					RetryAttempts = transport.RetryAttempts,
					Error = transport.Error,
				})
			];
		}
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
