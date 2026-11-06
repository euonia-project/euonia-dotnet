namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 应用程序关闭逻辑的上下文。
/// </summary>
public class ApplicationShutdownContext
{
	/// <summary>
	/// 获取可用于从依赖注入容器中解析服务的 <see cref="IServiceProvider"/>。
	/// </summary>
    public IServiceProvider ServiceProvider { get; }

	/// <summary>
	/// 初始化 <see cref="ApplicationShutdownContext"/> 类的新实例。
	/// </summary>
	/// <param name="serviceProvider">可用于解析服务的服务提供程序，不能为 <see langword="null"/>。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="serviceProvider"/> 为 <see langword="null"/> 时抛出。</exception>
    public ApplicationShutdownContext(IServiceProvider serviceProvider)
    {
        Check.EnsureNotNull(serviceProvider, nameof(serviceProvider));

        ServiceProvider = serviceProvider;
    }
}
