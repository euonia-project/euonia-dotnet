using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 通过类型上修饰的特性来评估该类型是单播消息（命令）、多播消息（事件）还是请求消息。
/// </summary>
public class AnnotationMessageConvention : IMessageConvention
{
	/// <summary>
	/// 获取消息约定的名称。
	/// </summary>
	public string Name { get; } = "Annotation decoration message convention";

	/// <summary>
	/// 判断指定的消息类型是否为单播消息（命令）。
	/// 通过检查消息类型是否标记了 <see cref="UnicastAttribute"/> 特性来判断。
	/// </summary>
	/// <param name="channel">消息通道名称；当 <paramref name="type"/> 为 <c>null</c> 时用于从通道注册信息中解析消息类型。</param>
	/// <param name="type">要检查的消息类型；为 <c>null</c> 时将从指定通道的注册信息中解析。</param>
	/// <returns>如果是单播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsUnicast(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		ArgumentNullException.ThrowIfNull(type);
		return type.GetCustomAttribute<UnicastAttribute>(false) != null;
	}

	/// <summary>
	/// 判断指定的消息类型是否为多播消息（事件）。
	/// 通过检查消息类型是否标记了 <see cref="MulticastAttribute"/> 特性来判断。
	/// </summary>
	/// <param name="channel">消息通道名称；当 <paramref name="type"/> 为 <c>null</c> 时用于从通道注册信息中解析消息类型。</param>
	/// <param name="type">要检查的消息类型；为 <c>null</c> 时将从指定通道的注册信息中解析。</param>
	/// <returns>如果是多播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsMulticast(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		ArgumentNullException.ThrowIfNull(type);
		return type.GetCustomAttribute<MulticastAttribute>(false) != null;
	}

	/// <summary>
	/// 判断指定的消息类型是否为请求消息。
	/// 通过检查消息类型是否标记了 <see cref="RequestAttribute"/> 特性来判断。
	/// </summary>
	/// <param name="channel">消息通道名称；当 <paramref name="type"/> 为 <c>null</c> 时用于从通道注册信息中解析消息类型。</param>
	/// <param name="type">要检查的消息类型；为 <c>null</c> 时将从指定通道的注册信息中解析。</param>
	/// <returns>如果是请求消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsRequest(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		ArgumentNullException.ThrowIfNull(type);
		return type.GetCustomAttribute<RequestAttribute>(false) != null;
	}
}