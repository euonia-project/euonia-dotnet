namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="EnvelopeTypeResolver"/> 的回归测试。
/// </summary>
public class EnvelopeTypeResolverTests
{
	/// <summary>
	/// 作为信封声明类型的多播接口，用于验证解析结果是声明类型而不是负载的运行时类型。
	/// </summary>
	private interface IBaseMessage : IMulticast
	{
	}

	/// <summary>
	/// 实现 <see cref="IBaseMessage"/> 的派生消息，用于验证按接口声明获取消息类型。
	/// </summary>
	private sealed class DerivedMessage : IBaseMessage
	{
	}

	/// <summary>
	/// 验证按信封声明的基类型或接口类型返回消息类型，而不是负载的运行时类型。
	/// </summary>
	[Fact]
	public void GetMessageType_ForBaseTypedEnvelope_ShouldReturnDeclaredType()
	{
		// 回归测试：按 Payload.GetType() 取派生类型后再强转 IMessageEnvelope<Derived> 会失败，
		// 导致以接口/基类类型发布的消息永远无法重投递。
		var envelope = new RoutedMessage<IBaseMessage>(new DerivedMessage(), "test.channel");

		var messageType = EnvelopeTypeResolver.GetMessageType(envelope);

		Assert.Equal(typeof(IBaseMessage), messageType);
	}
}
