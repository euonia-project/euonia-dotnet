namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义可访问 <see cref="IServiceProvider"/> 的模块化应用程序接口。
/// </summary>
public interface IApplicationWithServiceProvider : IModularityApplication, IAsyncDisposable
{
    /// <summary>
    /// 设置应用程序使用的 <see cref="IServiceProvider"/>。
    /// </summary>
    /// <param name="serviceProvider">应用程序的服务提供程序。</param>
    void SetServiceProvider(IServiceProvider serviceProvider);
    
    /// <summary>
    /// 使用指定的服务提供程序初始化应用程序。
    /// </summary>
    /// <remarks>
    /// 实现通常会设置服务提供程序并执行各模块的初始化逻辑。
    /// 该方法可安全地使用同一个服务提供程序重复调用，后续调用将被忽略。
    /// </remarks>
    /// <param name="serviceProvider">应用程序的服务提供程序。</param>
    void Initialize(IServiceProvider serviceProvider);

    /// <summary>
    /// 异步使用指定的服务提供程序初始化应用程序。
    /// </summary>
    /// <param name="serviceProvider">应用程序的服务提供程序。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>
    /// 未重写时回退到同步的 <see cref="Initialize"/>。
    /// 重写实现通常会设置服务提供程序并异步执行各模块的初始化逻辑。
    /// </remarks>
    Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        Initialize(serviceProvider);
        return Task.CompletedTask;
    }
}