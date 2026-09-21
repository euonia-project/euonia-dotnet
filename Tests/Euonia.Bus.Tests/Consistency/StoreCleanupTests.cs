using Nerosoft.Euonia.Bus.Tests.Consistency;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="IOutboxStore.Cleanup"/> 与 <see cref="IInboxStore.Cleanup"/> 保留策略的测试。
/// </summary>
/// <remarks>
/// 清理只应移除"已终结且已过期"的条目；任何仍在等待重试的条目都必须保留，否则会丢失消息。
/// </remarks>
public class StoreCleanupTests
{
	private static IMessageEnvelope<TestMessages.OrderPlacedEvent> CreateEnvelope(string messageId)
	{
		return TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/1" }, "test.events", messageId);
	}

	private static void Age(IOutboxStore store, string messageId, TimeSpan age)
	{
		store.Get(messageId).CreatedAt = DateTime.Now - age;
	}

	private static void Age(IInboxStore store, string messageId, TimeSpan age)
	{
		store.Get(messageId).CreatedAt = DateTime.Now - age;
	}

	[Fact]
	public void OutboxCleanup_RemovesExpiredSucceededEntry()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		store.Insert(CreateEnvelope("cleanup-outbox-done"), ["transport-a"]);
		store.MarkAsSuccess("cleanup-outbox-done", "transport-a");
		Age(store, "cleanup-outbox-done", TimeSpan.FromHours(2));

		store.Cleanup(DateTime.Now - TimeSpan.FromHours(1));

		Assert.Null(store.Get("cleanup-outbox-done"));
	}

	[Fact]
	public void OutboxCleanup_KeepsEntryThatIsNotYetExpired()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		store.Insert(CreateEnvelope("cleanup-outbox-fresh"), ["transport-a"]);
		store.MarkAsSuccess("cleanup-outbox-fresh", "transport-a");

		store.Cleanup(DateTime.Now - TimeSpan.FromHours(1));

		Assert.NotNull(store.Get("cleanup-outbox-fresh"));
	}

	/// <summary>
	/// 仍在等待重试的条目即使已过期也必须保留，否则会静默丢失待投递消息。
	/// </summary>
	[Fact]
	public void OutboxCleanup_KeepsExpiredFailedEntry()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		store.Insert(CreateEnvelope("cleanup-outbox-failed"), ["transport-a"]);
		store.MarkAsFailed("cleanup-outbox-failed", "transport-a", "boom");
		Age(store, "cleanup-outbox-failed", TimeSpan.FromHours(2));

		store.Cleanup(DateTime.Now - TimeSpan.FromHours(1));

		Assert.NotNull(store.Get("cleanup-outbox-failed"));
		Assert.Single(store.GetFailedMessages());
	}

	[Fact]
	public void OutboxCleanup_RemovesExpiredDeadLetteredEntry()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		store.Insert(CreateEnvelope("cleanup-outbox-dead"), ["transport-a"]);
		store.Get("cleanup-outbox-dead").GetTransport("transport-a").MarkAsDeadLettered("exhausted");
		Age(store, "cleanup-outbox-dead", TimeSpan.FromHours(2));

		store.Cleanup(DateTime.Now - TimeSpan.FromHours(1));

		Assert.Null(store.Get("cleanup-outbox-dead"));
	}

	[Fact]
	public void InboxCleanup_RemovesExpiredSucceededEntryAndKeepsFailedOne()
	{
		IInboxStore store = new InMemoryInboxStore();
		store.Insert("test.events", CreateEnvelope("cleanup-inbox-done"), ["handler-a"]);
		store.Insert("test.events", CreateEnvelope("cleanup-inbox-failed"), ["handler-a"]);
		store.MarkAsSuccess("cleanup-inbox-done", "handler-a");
		store.MarkAsFailed("cleanup-inbox-failed", "handler-a", "boom");
		Age(store, "cleanup-inbox-done", TimeSpan.FromHours(2));
		Age(store, "cleanup-inbox-failed", TimeSpan.FromHours(2));

		store.Cleanup(DateTime.Now - TimeSpan.FromHours(1));

		Assert.Null(store.Get("cleanup-inbox-done"));
		Assert.NotNull(store.Get("cleanup-inbox-failed"));
	}

	/// <summary>
	/// 默认实现为空操作，自定义存储实现无需改动即可继续工作。
	/// </summary>
	[Fact]
	public void Cleanup_DefaultImplementation_IsNoOp()
	{
		IOutboxStore store = new NoOpOutboxStore();
		store.Insert(CreateEnvelope("cleanup-noop"), ["transport-a"]);

		store.Cleanup(DateTime.MaxValue);

		Assert.NotNull(store.Get("cleanup-noop"));
	}

	/// <summary>
	/// 只实现必需成员的存储替身，用于验证 <see cref="IOutboxStore.Cleanup"/> 的默认实现。
	/// </summary>
	private sealed class NoOpOutboxStore : IOutboxStore
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
			return _inner.GetFailedMessages();
		}
	}
}
