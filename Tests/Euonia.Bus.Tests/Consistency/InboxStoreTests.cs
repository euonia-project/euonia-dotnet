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
	public void GetAndCache_ReturnsCachedEntryAndPopulatesStaticCache()
	{
		IInboxStore store = new InMemoryInboxStore();
		var message = CreateEnvelope("cache-1");

		store.Insert("test.events", message, ["handler-a"]);
		var first = store.GetAndCache("cache-1");
		var second = store.GetAndCache("cache-1");

		Assert.NotNull(first);
		Assert.Same(first, second);
		Assert.True(IInboxStore.Cache.TryGetValue("cache-1", out _));

		store.ClearCache();
		Assert.False(IInboxStore.Cache.TryGetValue("cache-1", out _));
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
}