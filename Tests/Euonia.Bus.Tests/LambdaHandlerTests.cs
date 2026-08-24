namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="LambdaHandler{TMessage, TResult}"/> 与 <see cref="LambdaHandler{TMessage}"/> 的测试。
/// </summary>
public class LambdaHandlerTests
{
	[Fact]
	public async Task TestHandlerWithResult_ShouldInvokeDelegateAndReturnResult()
	{
		var handler = new LambdaHandler<string, int>((message, _) => Task.FromResult(message.Length));

		var result = await handler.HandleAsync("hello", null);

		Assert.Equal(5, result);
	}

	[Fact]
	public async Task TestHandlerWithResult_ShouldPassMessageAndContext()
	{
		string receivedMessage = null;
		IMessageContext receivedContext = null;

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

	[Fact]
	public async Task TestHandlerWithoutResult_ShouldInvokeDelegate()
	{
		string receivedMessage = null;

		var handler = new LambdaHandler<string>((message, _) =>
		{
			receivedMessage = message;
			return Task.CompletedTask;
		});

		await handler.HandleAsync("world", null);

		Assert.Equal("world", receivedMessage);
	}

	private sealed class FakeMessageContext : IMessageContext
	{
		public string MessageId => "message-id";
		public string CorrelationId => "correlation-id";
		public string ConversationId => "conversation-id";
		public string RequestTraceId => "trace-id";
		public string Authorization => null;
		public System.Security.Principal.IPrincipal User => null;
		public IReadOnlyDictionary<string, string> Headers => new Dictionary<string, string>();
		public MessageMetadata Metadata => new();
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
