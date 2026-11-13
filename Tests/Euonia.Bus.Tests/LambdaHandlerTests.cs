namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="LambdaHandler{TMessage, TResult}"/> 与 <see cref="LambdaHandler{TMessage}"/> 的测试。
/// </summary>
public class LambdaHandlerTests
{
	/// <summary>
	/// 验证带结果的 lambda 处理器会执行委托并返回其计算结果。
	/// </summary>
	[Fact]
	public async Task TestHandlerWithResult_ShouldInvokeDelegateAndReturnResult()
	{
		var handler = new LambdaHandler<string, int>((message, _) => Task.FromResult(message.Length));

		var result = await handler.HandleAsync("hello", null!);

		Assert.Equal(5, result);
	}

	/// <summary>
	/// 验证 lambda 处理器把原始消息与上下文原样传给委托。
	/// </summary>
	[Fact]
	public async Task TestHandlerWithResult_ShouldPassMessageAndContext()
	{
		string? receivedMessage = null;
		IMessageContext? receivedContext = null;

		var handler = new LambdaHandler<string, string>((message, context) =>
		{
			receivedMessage = message;
			receivedContext = context;
			return Task.FromResult(message);
		});

		var context = new FakeMessageContext();
		await handler.HandleAsync("ping", context);

		Assert.Equal("ping", receivedMessage);
		Assert.Same(context, receivedContext);
	}

	/// <summary>
	/// 验证无结果的 lambda 处理器会执行委托并收到待处理消息。
	/// </summary>
	[Fact]
	public async Task TestHandlerWithoutResult_ShouldInvokeDelegate()
	{
		string? receivedMessage = null;

		var handler = new LambdaHandler<string>((message, _) =>
		{
			receivedMessage = message;
			return Task.CompletedTask;
		});

		await handler.HandleAsync("world", null!);

		Assert.Equal("world", receivedMessage);
	}

	/// <summary>
	/// 返回固定元数据的消息上下文替身，为 lambda 处理器提供非空上下文。
	/// </summary>
	private sealed class FakeMessageContext : IMessageContext
	{
		public string MessageId => "message-id";
		public string CorrelationId => "correlation-id";
		public string ConversationId => "conversation-id";
		public string RequestTraceId => "trace-id";
		public string Authorization => null!;
		public System.Security.Principal.IPrincipal User => null!;
		public IReadOnlyDictionary<string, string> Headers => new Dictionary<string, string>();
		public MessageMetadata Metadata => new();
		public string? Transport => null;
		public void Response<TMessage>(TMessage message)
		{
		}
		public void Failure(Exception exception)
		{
		}
		public void Dispose()
		{
		}
	}
}
