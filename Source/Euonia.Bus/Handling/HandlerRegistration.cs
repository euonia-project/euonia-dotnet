using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息处理程序的注册记录，包含处理程序的类型名称以及从服务提供程序创建处理实例的工厂。
/// </summary>
/// <param name="Name">处理程序的类型名称。</param>
/// <param name="Factory">用于创建处理实例的工厂委托。</param>
internal sealed record HandlerRegistration(string Name, HandlerFactory Factory);