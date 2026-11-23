using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 领域消息约定，根据消息类型判断其在总线上的路由方式（单播、多播或请求）。
/// </summary>
public sealed class DomainMessageConvention : IMessageConvention
{
	/// <summary>
	/// 判断消息类型是否为单播消息。
	/// </summary>
	/// <param name="channel">消息所在通道。</param>
	/// <param name="type">消息类型。</param>
	/// <returns>若类型实现了 <see cref="ICommand"/> 则返回 <c>true</c>，表示以单播方式路由。</returns>
	public bool IsUnicast(string channel, Type type)
	{
		return type.IsAssignableTo(typeof(ICommand));
	}

	/// <summary>
	/// 判断消息类型是否为多播消息。
	/// </summary>
	/// <param name="channel">消息所在通道。</param>
	/// <param name="type">消息类型。</param>
	/// <returns>若类型实现了 <see cref="IEvent"/> 则返回 <c>true</c>，表示以多播方式路由。</returns>
	public bool IsMulticast(string channel, Type type)
	{
		return type.IsAssignableTo(typeof(IEvent));
	}

	/// <summary>
	/// 判断消息类型是否为请求消息。
	/// </summary>
	/// <param name="channel">消息所在通道。</param>
	/// <param name="type">消息类型。</param>
	/// <returns>若类型实现了泛型 <see cref="IRequest{T}"/> 则返回 <c>true</c>。</returns>
	public bool IsRequest(string channel, Type type)
	{
		return type.IsAssignableToGeneric(typeof(IRequest<>));
	}

	/// <summary>
	/// 获取约定的名称。
	/// </summary>
	public string Name => nameof(DomainMessageConvention);
}
