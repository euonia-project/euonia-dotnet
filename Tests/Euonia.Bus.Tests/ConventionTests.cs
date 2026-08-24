namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对消息约定（<see cref="DefaultMessageConvention"/> 与 <see cref="AnnotationMessageConvention"/>）的测试。
/// </summary>
public class ConventionTests
{
	private sealed class UnicastMessage : IUnicast
	{
	}

	private sealed class MulticastMessage : IMulticast
	{
	}

	private sealed class RequestMessage : IRequest<int>
	{
	}

	private sealed class PlainMessage
	{
	}

	[Unicast]
	private sealed class AnnotatedUnicastMessage
	{
	}

	[Multicast]
	private sealed class AnnotatedMulticastMessage
	{
	}

	[Request(typeof(int))]
	private sealed class AnnotatedRequestMessage
	{
	}

	[Fact]
	public void TestDefaultConvention_IsUnicast_ShouldReturnTrue_ForUnicastMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsUnicast("default", typeof(UnicastMessage));

		Assert.True(result);
	}

	[Fact]
	public void TestDefaultConvention_IsUnicast_ShouldReturnFalse_ForPlainMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsUnicast("default", typeof(PlainMessage));

		Assert.False(result);
	}

	[Fact]
	public void TestDefaultConvention_IsMulticast_ShouldReturnTrue_ForMulticastMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsMulticast("default", typeof(MulticastMessage));

		Assert.True(result);
	}

	[Fact]
	public void TestDefaultConvention_IsRequest_ShouldReturnTrue_ForRequestMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsRequest("default", typeof(RequestMessage));

		Assert.True(result);
	}

	[Fact]
	public void TestDefaultConvention_IsRequest_ShouldReturnFalse_ForUnicastMessage()
	{
		var convention = new DefaultMessageConvention();

		var result = convention.IsRequest("default", typeof(UnicastMessage));

		Assert.False(result);
	}

	[Fact]
	public void TestDefaultConvention_ShouldThrow_WhenTypeIsNull()
	{
		var convention = new DefaultMessageConvention();

		Assert.Throws<ArgumentNullException>(() => convention.IsUnicast("default", null));
	}

	[Fact]
	public void TestAnnotationConvention_IsUnicast_ShouldReturnTrue_ForAnnotatedMessage()
	{
		var convention = new AnnotationMessageConvention();

		var result = convention.IsUnicast("default", typeof(AnnotatedUnicastMessage));

		Assert.True(result);
	}

	[Fact]
	public void TestAnnotationConvention_IsMulticast_ShouldReturnTrue_ForAnnotatedMessage()
	{
		var convention = new AnnotationMessageConvention();

		var result = convention.IsMulticast("default", typeof(AnnotatedMulticastMessage));

		Assert.True(result);
	}

	[Fact]
	public void TestAnnotationConvention_IsRequest_ShouldReturnTrue_ForAnnotatedMessage()
	{
		var convention = new AnnotationMessageConvention();

		var result = convention.IsRequest("default", typeof(AnnotatedRequestMessage));

		Assert.True(result);
	}

	[Fact]
	public void TestAnnotationConvention_IsUnicast_ShouldReturnFalse_ForPlainMessage()
	{
		var convention = new AnnotationMessageConvention();

		var result = convention.IsUnicast("default", typeof(PlainMessage));

		Assert.False(result);
	}

	[Fact]
	public void TestAnnotationConvention_ShouldThrow_WhenChannelIsNullOrWhiteSpace()
	{
		var convention = new AnnotationMessageConvention();

		Assert.Throws<ArgumentNullException>(() => convention.IsUnicast(null, typeof(PlainMessage)));
		Assert.Throws<ArgumentException>(() => convention.IsUnicast(" ", typeof(PlainMessage)));
	}
}
