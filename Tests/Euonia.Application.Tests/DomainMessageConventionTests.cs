using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 针对 <see cref="DomainMessageConvention"/> 的领域消息分类测试。
/// </summary>
public class DomainMessageConventionTests
{
	private readonly DomainMessageConvention _convention = new();

	/// <summary>
	/// 实现 <see cref="ICommand"/> 的类型应被识别为单播。
	/// </summary>
	[Fact]
	public void IsUnicast_Command_ReturnsTrue()
	{
		Assert.True(_convention.IsUnicast("channel", typeof(TestCommand)));
		Assert.False(_convention.IsMulticast("channel", typeof(TestCommand)));
		Assert.False(_convention.IsRequest("channel", typeof(TestCommand)));
	}

	/// <summary>
	/// 实现 <see cref="IEvent"/> 的类型应被识别为多播。
	/// </summary>
	[Fact]
	public void IsMulticast_Event_ReturnsTrue()
	{
		Assert.True(_convention.IsMulticast("channel", typeof(TestEvent)));
		Assert.False(_convention.IsUnicast("channel", typeof(TestEvent)));
		Assert.False(_convention.IsRequest("channel", typeof(TestEvent)));
	}

	/// <summary>
	/// 实现 <see cref="IRequest{T}"/> 的类型应被识别为请求。
	/// </summary>
	[Fact]
	public void IsRequest_Request_ReturnsTrue()
	{
		Assert.True(_convention.IsRequest("channel", typeof(TestRequest)));
		Assert.False(_convention.IsUnicast("channel", typeof(TestRequest)));
		Assert.False(_convention.IsMulticast("channel", typeof(TestRequest)));
	}

	/// <summary>
	/// 不实现任何领域消息接口的类型不应被识别为消息。
	/// </summary>
	[Fact]
	public void PlainType_ReturnsFalse()
	{
		Assert.False(_convention.IsUnicast("channel", typeof(object)));
		Assert.False(_convention.IsMulticast("channel", typeof(object)));
		Assert.False(_convention.IsRequest("channel", typeof(object)));
	}

	/// <summary>
	/// 检测方法应返回对应的消息约定类型。
	/// </summary>
	[Fact]
	public void Detect_ReturnsExpectedConventionType()
	{
		IMessageConvention convention = _convention;

		Assert.Equal(MessageConventionType.Unicast, convention.Detect("channel", typeof(TestCommand)));
		Assert.Equal(MessageConventionType.Multicast, convention.Detect("channel", typeof(TestEvent)));
		Assert.Equal(MessageConventionType.Request, convention.Detect("channel", typeof(TestRequest)));
	}

	/// <summary>
	/// 约定名称用于诊断。
	/// </summary>
	[Fact]
	public void Name_ReturnsTypeName()
	{
		Assert.Equal(nameof(DomainMessageConvention), _convention.Name);
	}

	private sealed class TestCommand : ICommand
	{
		public string CommandId => "command-1";
	}

	private sealed class TestEvent : IEvent
	{
		public string EventId => "event-1";

		public long Sequence { get; set; }

		public string EventIntent { get; set; } = string.Empty;

		public string OriginatorType { get; set; } = string.Empty;

		public string OriginatorId { get; set; } = string.Empty;
	}

	private sealed class TestRequest : IRequest<string>
	{
	}
}
