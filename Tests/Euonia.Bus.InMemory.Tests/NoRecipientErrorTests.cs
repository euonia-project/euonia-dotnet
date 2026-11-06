using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Bus.Tests.Requests;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对"无接收方"异常诊断信息的回归测试。
/// </summary>
/// <remarks>
/// 内存传输统一用 <see cref="MessagePack"/> 包装消息，此前的异常只报告包装类型，
/// 无法看出究竟是哪条业务消息缺少订阅者。
/// </remarks>
public class NoRecipientErrorTests
{
	private readonly IBus _bus;

	/// <summary>
	/// 使用测试宿主提供的服务提供程序解析消息总线。
	/// </summary>
	/// <param name="provider">由测试框架注入的服务提供程序。</param>
	public NoRecipientErrorTests(IServiceProvider provider)
	{
		_bus = provider.GetRequiredService<IBus>();
	}

	/// <summary>
	/// 验证调用无订阅者的通道时抛出 <see cref="MessageDeliverException"/>，且异常信息包含业务消息类型与通道名。
	/// </summary>
	/// <remarks>
	/// 错误信息不得只暴露内存传输使用的 <see cref="MessagePack"/> 包装类型，否则无法定位缺失订阅的消息。
	/// </remarks>
	[Fact]
	public async Task CallAsync_NoRecipient_ErrorIdentifiesMessageAndChannel()
	{
		const string channel = "missing.recipients.channel";

		var exception = await Assert.ThrowsAsync<MessageDeliverException>(() =>
			_bus.CallAsync(new UserCountRequest(), new CallOptions { Channel = channel }, cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(typeof(UserCountRequest).FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains(channel, exception.Message, StringComparison.Ordinal);
		Assert.DoesNotContain(typeof(MessagePack).FullName!, exception.Message, StringComparison.Ordinal);
	}
}
