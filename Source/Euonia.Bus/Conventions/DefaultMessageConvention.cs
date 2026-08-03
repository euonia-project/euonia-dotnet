namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 默认的消息约定。通过检查消息类型是否实现了对应的接口（<see cref="IUnicast"/>、<see cref="IMulticast"/>、<see cref="IRequest{TResponse}"/>）来判断消息的约定类型。
/// </summary>
public class DefaultMessageConvention : IMessageConvention
{
	/// <summary>
	/// 获取消息约定的名称。
	/// </summary>
	public string Name => "Default Message Convention";

	/// <summary>
	/// 根据指定的消息通道名称和消息类型判断是是否为单播消息。
	/// </summary>
	/// <remarks>通过检查消息类型是否实现了 <see cref="IUnicast"/> 接口来判断。</remarks>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是单播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsUnicast(string channel, Type type)
	{
		ArgumentNullException.ThrowIfNull(channel);
		if (type == null)
		{
			var registration = ChannelRegistrar.Get(channel)
			                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));
			type = registration.MessageType;
		}

		{
		}
		return type.IsAssignableTo(typeof(IUnicast)) && type != typeof(IUnicast);
	}

	/// <summary>
	/// 根据指定的消息通道名称和消息类型判断是是否为多播消息。
	/// </summary>
	/// <remarks>通过检查消息类型是否实现了 <see cref="IMulticast"/> 接口来判断。</remarks>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是多播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsMulticast(string channel, Type type)
	{
		ArgumentNullException.ThrowIfNull(channel);

		if (type == null)
		{
			var registration = ChannelRegistrar.Get(channel)
			                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));
			type = registration.MessageType;
		}

		{
		}

		return type.IsAssignableTo(typeof(IMulticast)) && type != typeof(IMulticast);
	}

	/// <summary>
	/// 根据指定的消息通道名称和消息类型判断是是否为请求消息。
	/// </summary>
	/// <remarks>通过检查消息类型是否实现了 <see cref="IRequest{TResponse}"/> 泛型接口来判断。</remarks>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是请求消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsRequest(string channel, Type type)
	{
		ArgumentNullException.ThrowIfNull(channel);

		if (type == null)
		{
			var registration = ChannelRegistrar.Get(channel)
			                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));
			type = registration.MessageType;
		}

		{
		}
		return type.IsAssignableToGeneric(typeof(IRequest<>));
	}
}