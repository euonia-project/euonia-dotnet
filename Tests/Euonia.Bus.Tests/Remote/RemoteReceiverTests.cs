using System.Text.Json;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="RemoteReceiver"/> 的回归测试。
/// </summary>
/// <remarks>
/// <see cref="MessageContext"/> 的事件由 <c>WeakEventManager</c> 支持，仅弱引用处理器目标。
/// <see cref="RemoteReceiver.ReceiveAsync"/> 曾用局部函数订阅回复事件且未持有强引用，
/// 一旦处理器执行期间发生 GC，闭包可能被回收，三个回调全部失效，
/// 导致 <c>await taskCompletion.Task</c> 永久挂起（HTTP / gRPC 调用永不返回）。
/// </remarks>
public class RemoteReceiverTests
{
	[Fact]
	public async Task ReceiveAsync_WhenHandlerTriggersGc_StillCompletesWithReply()
	{
		// 载荷中的类型名需可被 Type.GetType 解析，这里复用 Euonia.Bus 自身的类型。
		var payload = $$"""{"typeName":"{{typeof(RemoteReceiver).AssemblyQualifiedName}}"}""";
		var serializer = new FakeSerializer();
		var handler = new GcForcingHandlerContext("handled");

		var replyTask = RemoteReceiver.ReceiveAsync(serializer, handler, payload, TestContext.Current.CancellationToken);

		// 若弱订阅在 GC 后被回收，该任务将永不完成；用超时把它变成可断言的失败而非挂起。
		var reply = await replyTask.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		var parsed = JsonSerializer.Deserialize<RemoteReply<object>>(reply, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		Assert.NotNull(parsed);
		Assert.True(parsed.IsSuccess);
		// RemoteReply<TResult>.Result 声明为 object，经 System.Text.Json 反序列化后为 JsonElement。
		Assert.Equal("handled", parsed.Result?.ToString());
	}

	[Fact]
	public async Task ReceiveAsync_WhenHandlerThrows_ReturnsFailureReply()
	{
		var payload = $$"""{"typeName":"{{typeof(RemoteReceiver).AssemblyQualifiedName}}"}""";
		var serializer = new FakeSerializer();
		var handler = new GcForcingHandlerContext(new InvalidOperationException("handler exploded"));

		var reply = await RemoteReceiver.ReceiveAsync(serializer, handler, payload, TestContext.Current.CancellationToken)
		                                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		var parsed = JsonSerializer.Deserialize<RemoteReply<object>>(reply, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		Assert.NotNull(parsed);
		Assert.False(parsed.IsSuccess);
		Assert.Equal("handler exploded", parsed.Error.Message);
	}

	/// <summary>
	/// 在返回结果前强制触发一次完整 GC 的处理器上下文，用于模拟处理器执行期间发生垃圾回收。
	/// </summary>
	private sealed class GcForcingHandlerContext(object result) : IHandlerContext
	{
		public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed
		{
			add { }
			remove { }
		}

		public async Task<object> HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
		{
			// 制造真实的异步间隙，使 GC 恰好落在 RemoteReceiver 等待处理器的窗口内。
			await Task.Yield();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			if (result is Exception exception)
			{
				throw exception;
			}

			return result;
		}
	}

	/// <summary>
	/// 仅实现 <see cref="RemoteReceiver"/> 所需两个成员的序列化器替身，避免依赖真实 JSON 线格式。
	/// </summary>
	private sealed class FakeSerializer : IMessageSerializer
	{
		public IMessageEnvelope DeserializeEnvelope(string source, Type payloadType)
		{
			return new RoutedMessage<object>(new object(), "test.channel");
		}

		public string Serialize<T>(T source)
		{
			return JsonSerializer.Serialize(source);
		}

		public Task<byte[]> SerializeAsync<T>(T source, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<T> DeserializeAsync<T>(Stream source, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<T> DeserializeAsync<T>(byte[] source, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public T Deserialize<T>(byte[] source)
		{
			throw new NotSupportedException();
		}

		public T Deserialize<T>(Stream source)
		{
			throw new NotSupportedException();
		}

		public T Deserialize<T>(string source)
		{
			throw new NotSupportedException();
		}

		public object Deserialize(string source, Type type)
		{
			throw new NotSupportedException();
		}
	}
}
