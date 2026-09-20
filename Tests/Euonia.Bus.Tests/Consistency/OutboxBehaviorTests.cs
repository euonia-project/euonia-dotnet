using Nerosoft.Euonia.Bus.Behaviors;
using Nerosoft.Euonia.Bus.Tests.Consistency;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="OutgoingOutboxBehavior{TMessage, TResult}"/> 的测试。
/// </summary>
public class OutboxBehaviorTests
{
	[Fact]
	public async Task HandleAsync_OnSuccess_MarksTransportAsSuccess()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var envelope = (IMessageEnvelope<TestMessages.OrderPlacedEvent>)TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/1" }, "test.events", "behavior-1");
		store.Insert(envelope, ["transport-a"]);

		var behavior = new OutgoingOutboxBehavior<TestMessages.OrderPlacedEvent, Unit>(store, "transport-a");
		PipelineDelegate<IMessageEnvelope<TestMessages.OrderPlacedEvent>, Unit> next = _ => Task.FromResult(Unit.Value);

		var result = await behavior.HandleAsync(envelope, next);

		Assert.Equal(Unit.Value, result);
		var transport = store.Get("behavior-1").GetTransport("transport-a");
		Assert.Equal(OutboxTransportStatus.Success, transport.Status);
		Assert.Empty(store.GetFailedMessages());
	}

	[Fact]
	public async Task HandleAsync_OnFailure_MarksTransportAsFailedAndRethrows()
	{
		IOutboxStore store = new InMemoryOutboxStore();
		var envelope = (IMessageEnvelope<TestMessages.OrderPlacedEvent>)TestMessages.Envelope(new TestMessages.OrderPlacedEvent { OrderId = "orders/1" }, "test.events", "behavior-2");
		store.Insert(envelope, ["transport-a"]);

		var behavior = new OutgoingOutboxBehavior<TestMessages.OrderPlacedEvent, Unit>(store, "transport-a");
		PipelineDelegate<IMessageEnvelope<TestMessages.OrderPlacedEvent>, Unit> next = _ => throw new InvalidOperationException("transport is down");

		await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.HandleAsync(envelope, next));

		var transport = store.Get("behavior-2").GetTransport("transport-a");
		Assert.Equal(OutboxTransportStatus.Failed, transport.Status);
		Assert.Equal(1, transport.RetryAttempts);
		Assert.Equal("transport is down", transport.Error);
		var failed = Assert.Single(store.GetFailedMessages());
		Assert.Same(transport, failed);
	}
}