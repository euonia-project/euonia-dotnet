namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 事件参数类，用于表示通道注册事件的相关信息。
/// </summary>
public sealed class ChannelRegisteredEventArgs : EventArgs
{
	/// <summary>
	/// 初始化 <see cref="ChannelRegisteredEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="channel">已注册的通道名称。</param>
	/// <param name="type">与通道关联的消息类型。</param>
	/// <param name="handler">与通道关联的处理程序。</param>
	public ChannelRegisteredEventArgs(string channel, Type type, ChannelHandler handler)
	{
		Channel = channel;
		Type = type;
		Handler = handler;
	}

	/// <summary>
	/// 获取已注册的通道名称。
	/// </summary>
	public string Channel { get; }

	/// <summary>
	/// 获取与通道关联的消息类型。
	/// </summary>
	public Type Type { get; }

	/// <summary>
	/// 获取与通道关联的处理程序。
	/// </summary>
	public ChannelHandler Handler { get; }
}