namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义用于配置消息总线的配置器构建委托。
/// </summary>
/// <param name="configurator">要配置的 <see cref="IConfigurator"/> 实例。</param>
public delegate void ConfiguratorBuilder(IConfigurator configurator);