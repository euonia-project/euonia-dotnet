using Microsoft.Extensions.Logging.Abstractions;
using Nerosoft.Euonia.Bus.InMemory;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="InMemoryRecipient{TRecipient}.Receive"/> 投递语义的回归测试。
/// </summary>
/// <remarks>
/// <see cref="IMessenger"/> 的接收契约是同步的，但处理是异步的。
/// 此前的实现用 <c>AsyncContext.Run</c> 同步阻塞等待处理完成，导致每次发布都会阻塞调用方线程
/// 直到所有处理程序执行完毕。现在改为投递到接收者的串行后台泵。
/// </remarks>
public class InMemoryRecipientPumpTests
{
	[Fact]
	public async Task Receive_DoesNotBlockUntilHandlerCompletes()
	{
		var gate = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		var recipient = new InMemoryConsumer(new GatedHandlerContext(gate.Task), NullLoggerFactory.Instance);
		var pack = CreatePack("pump-1");

		var acknowledged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		recipient.MessageAcknowledged += (_, _) => acknowledged.TrySetResult(null);

		// 处理程序被 gate 挡住。若 Receive 仍同步等待处理完成，下面的等待会超时，
		// 从而把"再次引入阻塞"表现为一次明确的失败，而不是让整个测试宿主挂起。
		await Task.Run(() => recipient.Receive(pack), TestContext.Current.CancellationToken)
		          .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		Assert.False(acknowledged.Task.IsCompleted, "Receive must not wait for the handler to complete");

		// 放行处理程序后，确认事件应在泵内被触发。
		gate.SetResult(null);
		await acknowledged.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task Receive_ProcessesMessagesInOrder()
	{
		var order = new List<int>();
		var recipient = new InMemoryConsumer(new RecordingHandlerContext(order), NullLoggerFactory.Instance);

		for (var index = 0; index < 20; index++)
		{
			recipient.Receive(CreatePack($"pump-order-{index}", index));
		}

		// 泵是串行的：出队顺序即为处理顺序。
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
		while (order.Count < 20 && DateTime.UtcNow < deadline)
		{
			await Task.Delay(20, TestContext.Current.CancellationToken);
		}

		Assert.Equal(Enumerable.Range(0, 20), order);
	}

	private static MessagePack CreatePack(string messageId, int payload = 0)
	{
		var envelope = new RoutedMessage<int>(payload, "test.channel") { MessageId = messageId };
		return new MessagePack(envelope, new MessageContext(envelope));
	}

	/// <summary>
	/// 处理程序被 <see cref="TaskCompletionSource{TResult}"/> 挡住，直到测试显式放行。
	/// </summary>
	private sealed class GatedHandlerContext(Task gate) : IHandlerContext
	{
		public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed
		{
			add { }
			remove { }
		}

		public async Task<object> HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
		{
			await gate;
			return message;
		}
	}

	/// <summary>
	/// 记录处理顺序的处理程序上下文。
	/// </summary>
	private sealed class RecordingHandlerContext(List<int> order) : IHandlerContext
	{
		public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed
		{
			add { }
			remove { }
		}

		public Task<object> HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
		{
			lock (order)
			{
				order.Add((int)message);
			}

			return Task.FromResult(message);
		}
	}
}
