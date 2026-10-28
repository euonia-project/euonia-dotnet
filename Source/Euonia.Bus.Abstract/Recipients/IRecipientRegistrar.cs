namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义消息接收者注册器的接口。
/// </summary>
/// <remarks>
/// 注册器负责创建并启动传输层的接收者（broker 消费者 / 订阅者 / 请求执行器），
/// 因此也负责在停机时释放它们。实现必须释放 <see cref="RegisterAsync"/> 期间创建的
/// 全部接收者，否则其持有的连接、通道与会话将随进程存活而泄漏。
/// <para>
/// 接口同时继承 <see cref="IDisposable"/> 与 <see cref="IAsyncDisposable"/>：实现方只需提供
/// 同步的 <see cref="IDisposable.Dispose"/>，异步释放由接口默认实现转调该方法。
/// 这样注册器作为 DI 单例时，无论容器以 <c>Dispose()</c> 还是 <c>DisposeAsync()</c> 释放都能完成清理；
/// 若确需真正的异步清理，实现方可显式实现 <see cref="IAsyncDisposable.DisposeAsync"/>。
/// </para>
/// </remarks>
public interface IRecipientRegistrar : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// 注册消息接收者。
	/// </summary>
	/// <param name="registrations">要注册的通道注册信息集合。</param>
	/// <param name="defaultTransporter">默认传输器的名称。</param>
	/// <param name="cancellationToken">用于取消注册操作的令牌。</param>
	/// <returns>表示异步注册操作的任务。</returns>
	Task RegisterAsync(IDictionary<string, ChannelRegistration> registrations, string defaultTransporter, CancellationToken cancellationToken = default);

	/// <summary>
	/// 默认异步释放实现：直接调用同步 <see cref="IDisposable.Dispose"/>。
	/// </summary>
	ValueTask IAsyncDisposable.DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}
}