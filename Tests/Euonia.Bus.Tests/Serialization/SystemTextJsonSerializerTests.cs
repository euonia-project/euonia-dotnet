using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="SystemTextJsonSerializer"/> 消息负载序列化的回归测试。
/// </summary>
public class SystemTextJsonSerializerTests
{
	/// <summary>
	/// 验证以接口声明负载的信封序列化时采用负载的运行时类型，输出包含实际字段而非空对象。
	/// </summary>
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

	/// <summary>
	/// 声明 <see cref="Start"/> 的请求接口，用于验证序列化按信封声明类型而非运行时类型输出负载。
	/// </summary>
	private interface ICountRequest
	{
		int Start { get; }
	}

	/// <summary>
	/// 实现 <see cref="ICountRequest"/> 的请求负载，用于确认接口类型信封的负载字段不会被序列化为空对象。
	/// </summary>
	private sealed class CountRequest : ICountRequest
	{
		public int Start { get; init; }
	}
}
