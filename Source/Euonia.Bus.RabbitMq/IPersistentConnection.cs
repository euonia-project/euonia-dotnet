using RabbitMQ.Client;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// Defines the interface of a RabbitMQ persistent connection.
/// </summary>
public interface IPersistentConnection : IDisposable
{
	/// <summary>
	/// Gets a value indicating whether the connection is connected.
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// Tries to connect to RabbitMQ.
	/// </summary>
	/// <returns></returns>
	Task TryConnectAsync();

	/// <summary>
	/// Creates a channel.
	/// </summary>
	/// <returns></returns>
	Task<IChannel> CreateChannelAsync();

	/// <summary>
	/// Creates a channel with explicit options (for example publisher confirmations).
	/// </summary>
	/// <param name="options">Channel creation options.</param>
	/// <returns>表示异步操作的任务，包含创建的通道。</returns>
	/// <remarks>默认实现忽略选项并调用无参重载，便于既有自定义实现保持兼容。</remarks>
	Task<IChannel> CreateChannelAsync(CreateChannelOptions options)
	{
		return CreateChannelAsync();
	}
}