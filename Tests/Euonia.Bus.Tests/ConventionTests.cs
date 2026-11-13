namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对消息约定（<see cref="DefaultMessageConvention"/> 与 <see cref="AnnotationMessageConvention"/>）的测试。
/// </summary>
public class ConventionTests
{
	/// <summary>
	/// 实现 <see cref="IUnicast"/> 的测试消息，用于验证默认约定按接口判定单播。
	/// </summary>
	private sealed class UnicastMessage : IUnicast
	{
	}

	/// <summary>
	/// 实现 <see cref="IMulticast"/> 的测试消息，用于验证默认约定按接口判定多播。
	/// </summary>
	private sealed class MulticastMessage : IMulticast
	{
	}

	/// <summary>
	/// 实现 <see cref="IRequest{TResponse}"/> 的测试消息，用于验证默认约定按接口判定请求。
	/// </summary>
	private sealed class RequestMessage : IRequest<int>
	{
	}

	/// <summary>
	/// 未实现任何消息接口的普通类，用于验证约定不会把无关类型误判为消息。
	/// </summary>
	private sealed class PlainMessage
	{
	}

	/// <summary>
	/// 仅标记 <see cref="UnicastAttribute"/> 而未实现接口的消息，用于验证注解约定。
	/// </summary>
	[Unicast]
	private sealed class AnnotatedUnicastMessage
	{
	}

	/// <summary>
	/// 仅标记 <see cref="MulticastAttribute"/> 而未实现接口的消息，用于验证注解约定。
	/// </summary>
	[Multicast]
	private sealed class AnnotatedMulticastMessage
	{
	}

	/// <summary>
	/// 仅标记 <see cref="RequestAttribute"/> 而未实现接口的消息，用于验证注解约定。
	/// </summary>
	[Request(typeof(int))]
	private sealed class AnnotatedRequestMessage
	{
	}

	/// <summary>
	/// 验证默认约定把实现 <see cref="IUnicast"/> 的消息判定为单播，<c>IsUnicast</c> 返回 <c>true</c>。
	/// </summary>
	[Fact]
	public void TestDefaultConvention_IsUnicast_ShouldReturnTrue_ForUnicastMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsUnicast("default", typeof(UnicastMessage));

		Assert.True(result);
	}

	/// <summary>
	/// 验证默认约定不会把未实现任何消息接口的普通类判定为单播。
	/// </summary>
	[Fact]
	public void TestDefaultConvention_IsUnicast_ShouldReturnFalse_ForPlainMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsUnicast("default", typeof(PlainMessage));

		Assert.False(result);
	}

	/// <summary>
	/// 验证默认约定把实现 <see cref="IMulticast"/> 的消息判定为多播。
	/// </summary>
	[Fact]
	public void TestDefaultConvention_IsMulticast_ShouldReturnTrue_ForMulticastMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsMulticast("default", typeof(MulticastMessage));

		Assert.True(result);
	}

	/// <summary>
	/// 验证默认约定把实现 <see cref="IRequest{TResponse}"/> 的消息判定为请求。
	/// </summary>
	[Fact]
	public void TestDefaultConvention_IsRequest_ShouldReturnTrue_ForRequestMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsRequest("default", typeof(RequestMessage));

		Assert.True(result);
	}

	/// <summary>
	/// 验证单播消息不会被默认约定判定为请求。
	/// </summary>
	[Fact]
	public void TestDefaultConvention_IsRequest_ShouldReturnFalse_ForUnicastMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsRequest("default", typeof(UnicastMessage));

		Assert.False(result);
	}

	/// <summary>
	/// 验证默认约定在消息类型为 <c>null</c> 时抛出 <see cref="ArgumentNullException"/>。
	/// </summary>
	[Fact]
	public void TestDefaultConvention_ShouldThrow_WhenTypeIsNull()
	{
		var convention = new DefaultMessageConvention();

		Assert.Throws<ArgumentNullException>(() => convention.IsUnicast("default", null!));
	}

	/// <summary>
	/// 验证注解约定把标记了 <see cref="UnicastAttribute"/> 的消息判定为单播。
	/// </summary>
	[Fact]
	public void TestAnnotationConvention_IsUnicast_ShouldReturnTrue_ForAnnotatedMessage()
	{
		var convention = new AnnotationMessageConvention();

		var result = convention.IsUnicast("default", typeof(AnnotatedUnicastMessage));

		Assert.True(result);
	}

	/// <summary>
	/// 验证注解约定把标记了 <see cref="MulticastAttribute"/> 的消息判定为多播。
	/// </summary>
	[Fact]
	public void TestAnnotationConvention_IsMulticast_ShouldReturnTrue_ForAnnotatedMessage()
	{
		var convention = new AnnotationMessageConvention();

		var result = convention.IsMulticast("default", typeof(AnnotatedMulticastMessage));

		Assert.True(result);
	}

	/// <summary>
	/// 验证注解约定把标记了 <see cref="RequestAttribute"/> 的消息判定为请求。
	/// </summary>
	[Fact]
	public void TestAnnotationConvention_IsRequest_ShouldReturnTrue_ForAnnotatedMessage()
	{
		var convention = new AnnotationMessageConvention();

		var result = convention.IsRequest("default", typeof(AnnotatedRequestMessage));

		Assert.True(result);
	}

	/// <summary>
	/// 验证注解约定不会把未标记任何消息特性的普通类判定为单播。
	/// </summary>
	[Fact]
	public void TestAnnotationConvention_IsUnicast_ShouldReturnFalse_ForPlainMessage()
	{
		var convention = new AnnotationMessageConvention();

		var result = convention.IsUnicast("default", typeof(PlainMessage));

		Assert.False(result);
	}

	/// <summary>
	/// 验证注解约定在通道为 <c>null</c> 时抛 <see cref="ArgumentNullException"/>，为空白时抛 <see cref="ArgumentException"/>。
	/// </summary>
	[Fact]
	public void TestAnnotationConvention_ShouldThrow_WhenChannelIsNullOrWhiteSpace()
	{
		var convention = new AnnotationMessageConvention();

		Assert.Throws<ArgumentNullException>(() => convention.IsUnicast(null!, typeof(PlainMessage)));
		Assert.Throws<ArgumentException>(() => convention.IsUnicast(" ", typeof(PlainMessage)));
	}
}
