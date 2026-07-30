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
	/// 判断指定的消息通道是否为单播消息。
	/// 通过检查消息类型是否实现了 <see cref="IUnicast"/> 接口来判断。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <returns>如果是单播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsUnicast(string channel)
	{
		ArgumentNullException.ThrowIfNull(channel);

		var registration = ChannelRegistrar.Get(channel)
		                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));

		return registration.MessageType.IsAssignableTo(typeof(IUnicast)) && registration.MessageType != typeof(IUnicast);
	}

	/// <summary>
	/// 判断指定的消息通道是否为多播消息。
	/// 通过检查消息类型是否实现了 <see cref="IMulticast"/> 接口来判断。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <returns>如果是多播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsMulticast(string channel)
	{
		ArgumentNullException.ThrowIfNull(channel);

		var registration = ChannelRegistrar.Get(channel)
		                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));

		return registration.MessageType.IsAssignableTo(typeof(IMulticast)) && registration.MessageType != typeof(IMulticast);
	}

	/// <summary>
	/// 判断指定的消息通道是否为请求消息。
	/// 通过检查消息类型是否实现了 <see cref="IRequest{TResponse}"/> 泛型接口来判断。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <returns>如果是请求消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsRequest(string channel)
	{
		ArgumentNullException.ThrowIfNull(channel);

		var registration = ChannelRegistrar.Get(channel)
		                                   .GetOrThrow(() => new ChannelNotRegisterException(channel));

		return registration.MessageType.IsAssignableToGeneric(typeof(IRequest<>));
	}
}