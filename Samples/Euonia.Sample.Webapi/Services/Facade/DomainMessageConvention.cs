using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Facade;

/// <summary>
/// 提供一种约定，用于将消息 CLR 类型归类为消息基础设施所使用的消息类别。
/// </summary>
/// <remarks>
/// 该约定将领域标记接口映射到消息概念：
/// - 可赋值给 <c>ICommand</c> 的类型视为队列（命令）消息。
/// - 可赋值给 <c>IEvent</c> 的类型视为主题（事件）消息。
/// - 可赋值给泛型 <c>IRequest{TResponse}</c> 的类型视为请求消息。
/// </remarks>
internal class DomainMessageConvention : IMessageConvention
{
	/// <summary>
	/// 确定指定的类型是否表示命令（单播）消息。
	/// </summary>
	/// <param name="channel">消息通道名称；当前实现未使用该参数。</param>
	/// <param name="type">要评估的 CLR <see cref="Type"/>，不能为 <see langword="null"/>。</param>
	/// <returns>
	/// 若 <paramref name="type"/> 可赋值给 <see cref="ICommand"/>，则为 <see langword="true"/>；否则为 <see langword="false"/>。
	/// </returns>
	public bool IsUnicast(string channel, Type type)
	{
		return type.IsAssignableTo(typeof(ICommand));
	}

	/// <summary>
	/// 确定指定的类型是否表示事件（多播）消息。
	/// </summary>
	/// <param name="channel">消息通道名称；当前实现未使用该参数。</param>
	/// <param name="type">要评估的 CLR <see cref="Type"/>，不能为 <see langword="null"/>。</param>
	/// <returns>
	/// 若 <paramref name="type"/> 可赋值给 <see cref="IEvent"/>，则为 <see langword="true"/>；否则为 <see langword="false"/>。
	/// </returns>
	public bool IsMulticast(string channel, Type type)
	{
		return type.IsAssignableTo(typeof(IEvent));
	}

	/// <summary>
	/// 确定指定的类型是否表示请求消息。
	/// </summary>
	/// <param name="channel">消息通道名称；当前实现未使用该参数。</param>
	/// <param name="type">要评估的 CLR <see cref="Type"/>，不能为 <see langword="null"/>。</param>
	/// <returns>
	/// 若 <paramref name="type"/> 可赋值给泛型接口 <c>IRequest{TResponse}</c>，则为 <see langword="true"/>；否则为 <see langword="false"/>。
	/// </returns>
	public bool IsRequest(string channel, Type type)
	{
		return type.IsAssignableToGeneric(typeof(IRequest<>));
	}

	/// <summary>
	/// 获取此约定实现的名称。
	/// </summary>
	public string Name => nameof(DomainMessageConvention);
}
