using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="SystemTextJsonSerializer"/> 消息负载序列化的回归测试。
/// </summary>
public class SystemTextJsonSerializerTests
{
	[Fact]
	public void Serialize_EnvelopeWithInterfaceDeclaredPayload_ShouldUseRuntimePayload()
	{
		// 回归测试：STJ 按声明类型序列化，IMessageEnvelope<IRequest<T>> 的空接口负载
		// 曾被序列化为 "payload":{}，接收端只能反序列化出默认值。
		var serializer = new SystemTextJsonSerializer(Options.Create(new MessageSerializerOptions()));
		var payload = new CountRequest { Start = 41 };
		var envelope = new RoutedMessage<ICountRequest>(payload, "test.channel");

		var json = serializer.Serialize(envelope);

		Assert.Contains("\"start\":41", json, StringComparison.Ordinal);
		Assert.DoesNotContain("\"payload\":{}", json, StringComparison.Ordinal);
	}

	private interface ICountRequest
	{
		int Start { get; }
	}

	private sealed class CountRequest : ICountRequest
	{
		public int Start { get; init; }
	}
}
