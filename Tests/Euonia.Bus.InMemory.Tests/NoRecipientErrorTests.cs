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

	public NoRecipientErrorTests(IServiceProvider provider)
	{
		_bus = provider.GetRequiredService<IBus>();
	}

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
