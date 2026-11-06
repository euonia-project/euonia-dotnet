namespace Nerosoft.Euonia.Bus.Tests.Consistency;

/// <summary>
/// 测试用消息类型与信封工厂。
/// </summary>
internal static class TestMessages
{
	/// <summary>
	/// 一个测试用多播事件。
	/// </summary>
	public sealed class OrderPlacedEvent : IMulticast
	{
		/// <summary>
		/// 获取或设置订单标识，用于在断言中区分不同的消息实例。
		/// </summary>
		public string OrderId { get; set; } = string.Empty;
	}

	/// <summary>
	/// 构建一个可复用的路由信封。
	/// </summary>
	public static IMessageEnvelope<TMessage> Envelope<TMessage>(TMessage payload, string channel, string messageId)
	{
		return new RoutedMessage<TMessage>(payload, channel) { MessageId = messageId };
	}
}