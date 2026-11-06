namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 应用程序初始化的上下文。
/// </summary>
public class ApplicationInitializationContext : IServiceProviderAccessor
{
	/// <summary>
	/// 初始化 <see cref="ApplicationInitializationContext"/> 类的新实例。
	/// </summary>
	/// <param name="serviceProvider">应用程序初始化期间使用的服务提供程序。</param>
    public ApplicationInitializationContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

	/// <summary>
	/// 获取或设置 <see cref="IServiceProvider"/> 实例。
	/// </summary>
    public IServiceProvider ServiceProvider { get; set; }
}
