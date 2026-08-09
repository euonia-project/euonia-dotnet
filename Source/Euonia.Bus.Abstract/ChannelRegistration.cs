using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示特定通道和消息类型的处理器注册信息
/// </summary>
public sealed class ChannelRegistration
{
	private readonly List<ChannelHandler> _handlers = [];

	/// <summary>
	/// 使用指定的消息类型构造通道注册信息
	/// </summary>
	/// <param name="messageType">消息类型</param>
	public ChannelRegistration(Type messageType)
	{
		MessageType = messageType;
	}

	/// <summary>
	/// 获取消息类型
	/// </summary>
	/// <returns>消息类型</returns>
	public Type MessageType { get; }

	/// <summary>
	/// 获取处理器列表
	/// </summary>
	/// <returns>处理器列表</returns>
	public IList<ChannelHandler> Handlers => _handlers.AsReadOnly();

	/// <summary>
	/// 添加一个通道处理器。
	/// </summary>
	/// <param name="handler">要添加的通道处理器实例。</param>
	/// <returns>返回当前 <see cref="ChannelRegistration"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="handler"/> 为 <c>null</c> 时抛出。</exception>
	public ChannelRegistration AddHandler(ChannelHandler handler)
	{
		ArgumentNullException.ThrowIfNull(handler);
		_handlers.Add(handler);
		return this;
	}

	/// <summary>
	/// 通过处理器类型和方法名称添加一个通道处理器。
	/// </summary>
	/// <param name="handlerType">处理器的类型。</param>
	/// <param name="methodName">处理器的处理方法名称。</param>
	/// <returns>返回当前 <see cref="ChannelRegistration"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="handlerType"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="methodName"/> 为 <c>null</c> 或空白时抛出，或者当在指定类型中找不到该方法时抛出。</exception>
	public ChannelRegistration AddHandler(Type handlerType, string methodName)
	{
		ArgumentNullException.ThrowIfNull(handlerType);
		ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

		var method = handlerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (method == null)
		{
			throw new MissingMethodException(handlerType.FullName, methodName);
		}

		{
		}
		return AddHandler(new ChannelHandler(handlerType, method));
	}

	/// <summary>
	/// 通过处理器类型和方法信息添加一个通道处理器。
	/// </summary>
	/// <param name="handlerType">处理器的类型。</param>
	/// <param name="method">处理器的处理方法信息。</param>
	/// <returns>返回当前 <see cref="ChannelRegistration"/> 实例，以便进行链式调用。</returns>
	public ChannelRegistration AddHandler(Type handlerType, MethodInfo method)
	{
		return AddHandler(new ChannelHandler(handlerType, method));
	}
}