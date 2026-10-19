namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="EnvelopeTypeResolver"/> 的回归测试。
/// </summary>
public class EnvelopeTypeResolverTests
{
	private interface IBaseMessage : IMulticast
	{
	}

	private sealed class DerivedMessage : IBaseMessage
	{
	}

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
