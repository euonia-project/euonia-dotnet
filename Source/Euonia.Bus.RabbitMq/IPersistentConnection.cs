using RabbitMQ.Client;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// 定义 RabbitMQ 持久连接的接口。
/// </summary>
public interface IPersistentConnection : IDisposable
{
	/// <summary>
	/// 获取一个值，指示连接是否已建立。
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// 尝试连接到 RabbitMQ。
	/// </summary>
	/// <returns>表示异步连接过程的任务。</returns>
	Task TryConnectAsync();

	/// <summary>
	/// 创建通道。
	/// </summary>
	/// <returns>表示异步操作的任务，包含创建的通道。</returns>
	Task<IChannel> CreateChannelAsync();

	/// <summary>
	/// 使用显式选项创建通道（例如发布者确认）。
	/// </summary>
	/// <param name="options">通道创建选项。</param>
	/// <returns>表示异步操作的任务，包含创建的通道。</returns>
	/// <remarks>默认实现忽略选项并调用无参重载，便于既有自定义实现保持兼容。</remarks>
	Task<IChannel> CreateChannelAsync(CreateChannelOptions? options)
	{
		return CreateChannelAsync();
	}
}