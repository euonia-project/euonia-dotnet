namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示标记的类是请求消息
/// </summary>
/// <remarks>
/// 此Attribute用于标记消息类为请求消息，表示该消息期望收到一个响应。使用此特性可以在消息总线中实现请求-响应通信模式，允许发送方发送请求消息并接收相应的响应。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RequestAttribute : TransportableAttribute
{
	/// <summary>
	/// 初始化 <see cref="RequestAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="responseType">请求所期望的响应类型。</param>
	public RequestAttribute(Type responseType)
	{
		ResponseType = responseType;
	}

	/// <summary>
	/// 获取请求所期望的响应类型。
	/// </summary>
	public Type ResponseType { get; }
}