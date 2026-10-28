using Apache.NMS;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 定义了持久化连接的接口，用于管理与消息代理的连接生命周期。
/// </summary>
public interface IPersistentConnection : IDisposable
{
	/// <summary>
	/// 在底层连接重新建立后触发；接收者应据此重建会话与消费者。
	/// </summary>
	event EventHandler Reconnected
	{
		add { }
		remove { }
	}

	/// <summary>
	/// 获取一个值，指示连接是否已建立。
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// 尝试异步建立与消息代理的连接。
	/// </summary>
	/// <returns>表示异步操作的任务。</returns>
	Task TryConnectAsync();

	/// <summary>
	/// 异步创建一个会话。
	/// </summary>
	/// <returns>表示异步操作的任务，任务结果包含创建的会话。</returns>
	Task<ISession> CreateSessionAsync();
}