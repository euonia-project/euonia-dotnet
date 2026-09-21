using Nerosoft.Euonia.Bus.Tests.Consistency;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="InboxEntry"/> 与 <see cref="IInboxStore"/>（含默认方法）的测试。
/// </summary>
public class InboxStoreTests
{
	private static IMessageEnvelope CreateEnvelope(string messageId = "inbox-1")
	{
		return TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/1" }, "test.events", messageId);
	}

	[Fact]
	public void Insert_WithHandlers_AddsPendingHandlerForEachName()
	{
		IInboxStore store = new InMemoryInboxStore();
		var message = CreateEnvelope();

		var inserted = store.Insert("test.events", message, ["handler-a", "handler-b"]);

		Assert.True(inserted);
		var entry = store.Get(message.MessageId);
		Assert.NotNull(entry);
		Assert.Equal(message.MessageId, entry.MessageId);
		Assert.Equal("test.events", entry.Channel);
		Assert.Equal(typeof(TestMessages.OrderPlacedEvent).FullName, entry.MessageType);
		Assert.Same(message, entry.Content);
		Assert.Equal(2, entry.Handlers.Count);
		Assert.All(entry.Handlers, handler =>
		{
			Assert.Equal(message.MessageId, handler.MessageId);
			Assert.Equal(InboxHandlerStatus.Pending, handler.Status);
			Assert.Equal(0, handler.RetryAttempts);
		});
		Assert.Equal(["handler-a", "handler-b"], entry.Handlers.Select(handler => handler.Name));
	}

	[Fact]
	public void Insert_DuplicateMessageId_ReturnsFalse()
	{
		IInboxStore store = new InMemoryInboxStore();
		var message = CreateEnvelope("duplicate-1");

		Assert.True(store.Insert("test.events", message, ["handler-a"]));
		Assert.False(store.Insert("test.events", message, ["handler-a"]));
	}

	[Fact]
	public void MarkAsSuccess_UpdatesHandlerStatus()
	{
		IInboxStore store = new InMemoryInboxStore();
		var message = CreateEnvelope();

		store.Insert("test.events", message, ["handler-a", "handler-b"]);
		store.MarkAsSuccess(message.MessageId, "handler-a");

		var handlerA = store.Get(message.MessageId).GetHandler("handler-a");
		var handlerB = store.Get(message.MessageId).GetHandler("handler-b");

		Assert.Equal(InboxHandlerStatus.Success, handlerA.Status);
		Assert.Equal(InboxHandlerStatus.Pending, handlerB.Status);
		Assert.DoesNotContain(handlerA, store.GetFailedMessages());
		Assert.DoesNotContain(handlerB, store.GetFailedMessages());
	}

	[Fact]
	public void GetFailedMessages_ExcludesPendingInFlightHandlers()
	{
		IInboxStore store = new InMemoryInboxStore();
		var message = CreateEnvelope("inflight-1");

		store.Insert("test.events", message, ["handler-a"]);

		// 尚未标记失败（仍在执行中/未被调度器接管）的记录不应被返回，避免重复执行破坏去重。
		Assert.Empty(store.GetFailedMessages());
	}

	[Fact]
	public void MarkAsFailed_UpdatesStatusAndIsIncludedInFailedMessages()
	{
		IInboxStore store = new InMemoryInboxStore();
		var message = CreateEnvelope();

		store.Insert("test.events", message, ["handler-a"]);
		store.MarkAsFailed(message.MessageId, "handler-a", "handler threw");

		var handler = store.Get(message.MessageId).GetHandler("handler-a");
		Assert.Equal(InboxHandlerStatus.Failed, handler.Status);
		Assert.Equal(1, handler.RetryAttempts);
		Assert.Equal("handler threw", handler.Error);

		var failed = Assert.Single(store.GetFailedMessages());
		Assert.Same(handler, failed);
	}

	[Fact]
	public void GetAndCache_ReturnsCachedEntryOnSecondCall()
	{
		var counter = new CountingInboxStore();
		IInboxStore store = counter;
		var message = CreateEnvelope("cache-1");

		store.Insert("test.events", message, ["handler-a"]);
		var first = store.GetAndCache("cache-1");
		var second = store.GetAndCache("cache-1");

		Assert.NotNull(first);
		Assert.Same(first, second);
		// 第二次应命中缓存，不再回查底层存储。
		Assert.Equal(1, counter.GetCallCount);

		store.ClearCache();
		store.GetAndCache("cache-1");
		Assert.Equal(2, counter.GetCallCount);
	}

	/// <summary>
	/// 回归测试：缓存曾以接口上的 <c>static</c> 字段实现，导致进程内所有存储实例共享同一份缓存，
	/// 且仅以消息标识符为键，不同数据库 / 租户 / 并行测试之间会互相读到对方的条目。
	/// </summary>
	[Fact]
	public void GetAndCache_DoesNotLeakAcrossStoreInstances()
	{
		IInboxStore populated = new CountingInboxStore();
		IInboxStore empty = new CountingInboxStore();

		populated.Insert("test.events", CreateEnvelope("shared-id"), ["handler-a"]);

		Assert.NotNull(populated.GetAndCache("shared-id"));

		// empty 中并不存在该消息；若缓存跨实例共享，此处会错误地返回 populated 的条目。
		Assert.Null(empty.GetAndCache("shared-id"));
	}

	[Fact]
	public void GetAndCache_WithMissingMessageId_ReturnsNull()
	{
		IInboxStore store = new InMemoryInboxStore();
		Assert.Null(store.GetAndCache("missing"));
	}

	[Fact]
	public void InMemoryInboxStore_MarkWithoutEntry_IsNoOp()
	{
		IInboxStore store = new InMemoryInboxStore();
		store.MarkAsSuccess("missing", "handler-a");
		store.MarkAsFailed("missing", "handler-a", "error");
		Assert.Empty(store.GetFailedMessages());
	}

	/// <summary>
	/// 统计 <see cref="IInboxStore.Get"/> 调用次数的存储替身，用于观察缓存是否真正生效。
	/// </summary>
	/// <remarks>
	/// 直接实现接口而非继承 <see cref="InMemoryInboxStore"/>：<c>GetAndCache</c> 是默认接口方法，
	/// 通过接口槽位调用 <c>Get</c>，因此只能通过接口实现来拦截计数。
	/// </remarks>
	private sealed class CountingInboxStore : IInboxStore
	{
		private readonly Dictionary<string, InboxEntry> _entries = [];

		public int GetCallCount { get; private set; }

		public bool Insert(InboxEntry entry)
		{
			return _entries.TryAdd(entry.MessageId, entry);
		}

		public InboxEntry Get(string messageId)
		{
			GetCallCount++;
			return _entries.GetValueOrDefault(messageId);
		}

		public void MarkAsSuccess(string messageId, string handler)
		{
		}

		public void MarkAsFailed(string messageId, string handler, string errorMessage)
		{
		}

		public IReadOnlyList<InboxHandler> GetFailedMessages()
		{
			return [];
		}
	}
}