namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义一组用于判断消息类型为请求消息、多播消息还是单播消息的约定。
/// </summary>
public interface IMessageConvention
{
	/// <summary>
	/// 获取约定的名称，用于诊断目的。
	/// </summary>
	string Name { get; }

	/// <summary>
	/// 判断指定的消息类型是否为单播消息。
	/// </summary>
	/// <param name="channel">要检查的通道名称；当 <paramref name="type"/> 为 <c>null</c> 时用于解析消息类型。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果 <paramref name="type"/> 表示单播消息，则为 <c>true</c>。</returns>
	/// <remarks>
	/// 单播消息仅传递给单个接收者，通常使用基于队列的机制。
	/// </remarks>
	bool IsUnicast(string channel, Type type);

	/// <summary>
	/// 判断指定的消息类型是否为多播消息。
	/// </summary>
	/// <param name="channel">要检查的通道名称；当 <paramref name="type"/> 为 <c>null</c> 时用于解析消息类型。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果 <paramref name="type"/> 表示多播消息，则为 <c>true</c>。</returns>
	/// <remarks>
	/// 多播消息传递给多个接收者，通常使用基于主题的机制。
	/// </remarks>
	bool IsMulticast(string channel, Type type);

	/// <summary>
	/// 判断指定的消息类型是否为请求消息。
	/// </summary>
	/// <param name="channel">要检查的通道名称；当 <paramref name="type"/> 为 <c>null</c> 时用于解析消息类型。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果 <paramref name="type"/> 表示请求消息，则为 <c>true</c>。</returns>
	/// <remarks>
	/// 请求消息发送给单个接收者，并期望收到响应。
	/// </remarks>
	bool IsRequest(string channel, Type type);

	/// <summary>
	/// 根据通道名称和消息类型检测消息约定类型。
	/// </summary>
	/// <param name="channel">要检查的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>返回检测到的消息约定类型。</returns>
	MessageConventionType Detect(string channel, Type type)
	{
		var flag = 0;

		if (IsMulticast(channel, type))
		{
			flag |= 1;
		}

		if (IsUnicast(channel, type))
		{
			flag |= 2;
		}

		if (IsRequest(channel, type))
		{
			flag |= 4;
		}

		return flag switch
		{
			1 => MessageConventionType.Multicast,
			2 => MessageConventionType.Unicast,
			4 => MessageConventionType.Request,
			_ => throw new MessageTypeException($"The message type {type.AssemblyQualifiedName} is not a queue/topic/request type.")
		};
	}
}