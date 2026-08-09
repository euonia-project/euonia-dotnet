namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示消息通道未注册的异常。
/// </summary>
/// <remarks>
///	<see cref="ChannelNotRegisterException"/> 是一个自定义异常类，用于表示在消息总线中尝试访问未注册的消息通道时发生的错误。该异常继承自 <see cref="Exception"/> 类，并提供了通道名称和错误消息的属性，以便在捕获异常时获取相关信息。
/// </remarks>
public class ChannelNotRegisterException : Exception
{
	/// <summary>
	/// 构造一个新的 <see cref="ChannelNotRegisterException"/>，并指定未注册的通道名称。
	/// </summary>
	/// <param name="channel">未注册的通道名称。</param>
	public ChannelNotRegisterException(string channel)
		: this(channel, $"Channel '{channel}' is not registered")
	{
		Channel = channel;
	}

	/// <summary>
	/// 构造一个新的 <see cref="ChannelNotRegisterException"/>，并指定未注册的通道名称和详细消息。
	/// </summary>
	/// <param name="channel">未注册的通道名称。</param>
	/// <param name="message">详细的错误消息。</param>
	public ChannelNotRegisterException(string channel, string message)
		: base(message)
	{
		Channel = channel;
	}

	/// <summary>
	/// 获取未注册的消息通道名称
	/// </summary>
	/// <returns>未注册的消息通道名称。</returns>
	public string Channel { get; }
}