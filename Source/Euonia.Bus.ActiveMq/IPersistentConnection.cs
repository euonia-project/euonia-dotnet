using Apache.NMS;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 定义了持久化连接的接口，用于管理与消息代理的连接生命周期。
/// </summary>
public interface IPersistentConnection : IDisposable
{
	/// <summary>
	/// Gets a value indicating whether the connection is connected.
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// Attempts to establish a connection to the message broker asynchronously.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the connection was successful.</returns>
	Task TryConnectAsync();

	/// <summary>
	/// Creates a session asynchronously.
	/// </summary>
	/// <returns>The created session.</returns>
	Task<ISession> CreateSessionAsync();
}