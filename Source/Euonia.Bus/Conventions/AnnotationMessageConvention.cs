using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 通过类型上修饰的特性来评估该类型是消息、命令还是事件。
/// </summary>
public class AnnotationMessageConvention : IMessageConvention
{
	/// <summary>
	/// 获取消息约定的名称。
	/// </summary>
	public string Name { get; } = "Annotation decoration message convention";

	/// <summary>
	/// 判断指定的消息通道是否为单播消息（命令）。
	/// 通过检查消息类型是否标记了 <see cref="UnicastAttribute"/> 特性来判断。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是单播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsUnicast(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		if (type == null)
		{
			var registration = ChannelRegistrar.Get(channel)
			                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));
			type = registration.MessageType;
		}

		{
		}
		return type.GetCustomAttribute<UnicastAttribute>(false) != null;
	}

	/// <summary>
	/// 判断指定的消息通道是否为多播消息（事件）。
	/// 通过检查消息类型是否标记了 <see cref="MulticastAttribute"/> 特性来判断。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是多播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsMulticast(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		if (type == null)
		{
			var registration = ChannelRegistrar.Get(channel)
			                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));
			type = registration.MessageType;
		}

		{
		}
		return type.GetCustomAttribute<MulticastAttribute>(false) != null;
	}

	/// <summary>
	/// 判断指定的消息类型是否为请求消息。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是请求消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsRequest(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		if (type == null)
		{
			var registration = ChannelRegistrar.Get(channel)
			                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));
			type = registration.MessageType;
		}

		{
		}
		return type.GetCustomAttribute<RequestAttribute>(false) != null;
	}
}