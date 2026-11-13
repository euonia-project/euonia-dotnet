using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Domain.Etos;

namespace Nerosoft.Euonia.Sample.Facade.Subscribers;

/// <summary>
/// 验证码创建事件的订阅者，负责输出验证码内容以演示消息投递。
/// </summary>
public class MessageSendSubscriber : ITransientDependency
{
	/// <summary>
	/// 处理验证码创建事件，打印验证码的渠道、内容、接收方、用途与有效期。
	/// </summary>
	/// <param name="eto">验证码创建事件数据。</param>
	/// <param name="cancellationToken">用于取消异步处理的取消标记。</param>
	[Subscribe("nerosoft.chalky.eto:OnetimeCodeCreated")]
	public async Task HandleOtpCreatedAsync(OnetimeCodeCreatedEto eto, CancellationToken cancellationToken = default)
	{
		Console.WriteLine($"[MessageSendSubscriber] Received OTP: Type={eto.Type}, Code={eto.Code}, Recipient={eto.Recipient}, Usage={eto.Usage}, Timeout={eto.Timeout}");
	}
}