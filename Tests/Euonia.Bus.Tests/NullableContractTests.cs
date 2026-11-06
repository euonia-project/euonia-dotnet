using Nerosoft.Euonia.Bus.Tests.Consistency;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对消息信封可空契约的回归测试。
/// </summary>
/// <remarks>
/// 这些成员（请求追踪标识、授权信息、用户主体）在请求作用域外或外部消息未携带时
/// 本来就可能为 <c>null</c>；契约必须如实声明，避免调用方在远离源头的位置触发 NRE。
/// </remarks>
public class NullableContractTests
{
	[Fact]
	public void RoutedMessage_WithoutRequestContext_ExposesNulls()
	{
		IMessageEnvelope envelope = new RoutedMessage<TestMessages.OrderPlacedEvent>(new(), "test");

		Assert.Null(envelope.RequestTraceId);
		Assert.Null(envelope.Authorization);
		Assert.Null(envelope.User);
	}

	[Fact]
	public void MessageContext_FromEnvelopeWithoutValues_ExposesNulls()
	{
		var message = new RoutedMessage<TestMessages.OrderPlacedEvent>(new(), "test");
		using var context = new MessageContext(message);

		Assert.Null(context.RequestTraceId);
		Assert.Null(context.Authorization);
		Assert.Null(context.User);
	}

	[Fact]
	public void InboundEnvelope_WithoutUser_ExposesNull()
	{
		var message = new RoutedMessage<TestMessages.OrderPlacedEvent>(new(), "test");
		using var context = new MessageContext(message);
		var envelope = new InboundEnvelope(context, "test", new object());

		Assert.Null(envelope.RequestTraceId);
		Assert.Null(envelope.Authorization);
		Assert.Null(envelope.User);
	}

	[Fact]
	public void MessageMetadata_MissingKey_ReturnsNull()
	{
		var metadata = new MessageMetadata();

		Assert.Null(metadata["missing"]);
	}

	[Fact]
	public void MessageRepliedEventArgs_AllowsNullResult()
	{
		var args = new MessageRepliedEventArgs(null);

		Assert.Null(args.Result);
	}

	[Fact]
	public void MessageContext_CompleteWithoutMessageId_ReportsNull()
	{
		using var context = new MessageContext();
		string? completedMessageId = "not-raised";
		EventHandler<MessageHandledEventArgs> handler = (_, args) => completedMessageId = args.MessageId;

		context.Completed += handler;
		context.Complete(null);

		Assert.Null(completedMessageId);
	}
}
