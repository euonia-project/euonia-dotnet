namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义可访问 <see cref="IServiceProvider"/> 的模块化应用程序接口。
/// </summary>
public interface IApplicationWithServiceProvider: IModularityApplication
{
    /// <summary>
    /// 设置应用程序使用的 <see cref="IServiceProvider"/>。
    /// </summary>
    /// <param name="serviceProvider">应用程序的服务提供程序。</param>
    void SetServiceProvider(IServiceProvider serviceProvider);
    
    /// <summary>
    /// 使用指定的服务提供程序初始化应用程序。
    /// </summary>
    /// <remarks>实现通常会设置服务提供程序并执行各模块的初始化逻辑。</remarks>
    /// <param name="serviceProvider">应用程序的服务提供程序。</param>
    void Initialize(IServiceProvider serviceProvider);
}