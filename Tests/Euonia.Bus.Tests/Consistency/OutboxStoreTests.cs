using Nerosoft.Euonia.Bus.Tests.Consistency;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="OutboxEntry"/> 与 <see cref="IOutboxStore"/>（含默认方法）的测试。
/// </summary>
public class OutboxStoreTests
{
	private static IMessageEnvelope<TestMessages.OrderPlacedEvent> CreateEnvelope(string messageId = "outbox-1")
	{
		return TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/1" }, "test.events", messageId);
	}

	[Fact]
	public void Insert_WithTransports_AddsPendingTransportForEachName()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var message = CreateEnvelope();

		var inserted = store.Insert(message, ["transport-a", "transport-b"]);

		Assert.True(inserted);
		var entry = store.Get(message.MessageId);
		Assert.NotNull(entry);
		Assert.Equal(message.MessageId, entry.MessageId);
		Assert.Equal("test.events", entry.Channel);
		Assert.Equal(typeof(TestMessages.OrderPlacedEvent).FullName, entry.MessageType);
		Assert.Same(message, entry.Content);
		Assert.Equal(2, entry.Transports.Count);
		Assert.All(entry.Transports, transport =>
		{
			Assert.Equal(message.MessageId, transport.MessageId);
			Assert.Equal(OutboxTransportStatus.Pending, transport.Status);
			Assert.Equal(0, transport.RetryAttempts);
		});
		Assert.Equal(["transport-a", "transport-b"], entry.Transports.Select(transport => transport.Name));
	}

	[Fact]
	public void Insert_DuplicateMessageId_ReturnsFalse()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var message = CreateEnvelope("duplicate-1");

		Assert.True(store.Insert(message, ["transport-a"]));
		Assert.False(store.Insert(message, ["transport-a"]));
	}

	[Fact]
	public void MarkAsSuccess_UpdatesTransportStatus()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var message = CreateEnvelope();

		store.Insert(message, ["transport-a", "transport-b"]);
		store.MarkAsSuccess(message.MessageId, "transport-a");

		var transportA = store.Get(message.MessageId).GetTransport("transport-a");
		var transportB = store.Get(message.MessageId).GetTransport("transport-b");

		Assert.Equal(OutboxTransportStatus.Success, transportA.Status);
		Assert.Equal(OutboxTransportStatus.Pending, transportB.Status);
		Assert.DoesNotContain(transportA, store.GetFailedMessages());
		Assert.DoesNotContain(transportB, store.GetFailedMessages());
	}

	[Fact]
	public void GetFailedMessages_ExcludesPendingInFlightTransports()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var message = CreateEnvelope("inflight-1");

		store.Insert(message, ["transport-a"]);

		// 尚未标记失败（仍在投递中/未被调度器接管）的条目不应被返回，避免重复投递。
		Assert.Empty(store.GetFailedMessages());
	}

	[Fact]
	public void MarkAsFailed_UpdatesStatusAndIsIncludedInFailedMessages()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var message = CreateEnvelope();

		store.Insert(message, ["transport-a"]);
		store.MarkAsFailed(message.MessageId, "transport-a", "connection lost");

		var transport = store.Get(message.MessageId).GetTransport("transport-a");
		Assert.Equal(OutboxTransportStatus.Failed, transport.Status);
		Assert.Equal(1, transport.RetryAttempts);
		Assert.Equal("connection lost", transport.Error);

		var failed = Assert.Single(store.GetFailedMessages());
		Assert.Same(transport, failed);
	}

	[Fact]
	public void GetAndCache_ReturnsCachedEntryAndPopulatesStaticCache()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var message = CreateEnvelope("cache-1");

		store.Insert(message, ["transport-a"]);
		var first = store.GetAndCache("cache-1");
		var second = store.GetAndCache("cache-1");

		Assert.NotNull(first);
		Assert.Same(first, second);
		Assert.True(IOutboxStore.Cache.TryGetValue("cache-1", out _));

		store.ClearCache();
		Assert.False(IOutboxStore.Cache.TryGetValue("cache-1", out _));
	}

	[Fact]
	public void GetAndCache_WithMissingMessageId_ReturnsNull()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		Assert.Null(store.GetAndCache("missing"));
	}

	[Fact]
	public void OutboxEntry_GetTransport_ReturnsMatchingTransport()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var message = CreateEnvelope();

		store.Insert(message, ["transport-a"]);
		var entry = store.Get(message.MessageId);

		Assert.NotNull(entry.GetTransport("transport-a"));
		Assert.Null(entry.GetTransport("transport-z"));
	}

	[Fact]
	public void InMemoryOutboxStore_MarkWithoutEntry_IsNoOp()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		store.MarkAsSuccess("missing", "transport-a");
		store.MarkAsFailed("missing", "transport-a", "error");
		Assert.Empty(store.GetFailedMessages());
	}
}