namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于创建消息处理程序的委托。
/// </summary>
/// <param name="provider">用于解析处理程序依赖的服务提供程序。</param>
/// <returns>创建的消息处理程序委托。</returns>
public delegate HandlerDelegate HandlerFactory(IServiceProvider provider);