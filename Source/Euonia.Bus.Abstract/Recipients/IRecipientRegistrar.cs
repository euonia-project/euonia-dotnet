namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义消息接收者注册器的接口。
/// </summary>
public interface IRecipientRegistrar
{
	/// <summary>
	/// 注册消息接收者。
	/// </summary>
	/// <param name="registrations">要注册的通道注册信息集合。</param>
	/// <param name="defaultTransporter">默认传输器的名称。</param>
	/// <param name="cancellationToken">用于取消注册操作的令牌。</param>
	/// <returns>表示异步注册操作的任务。</returns>
	Task RegisterAsync(IDictionary<string, ChannelRegistration> registrations, string defaultTransporter, CancellationToken cancellationToken = default);
}