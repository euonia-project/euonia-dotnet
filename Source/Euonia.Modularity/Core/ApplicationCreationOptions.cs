using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 应用程序创建选项。
/// </summary>
public class ApplicationCreationOptions
{
    /// <summary>
    /// 初始化 <see cref="ApplicationCreationOptions"/> 类的新实例。
    /// </summary>
    /// <param name="services">应用程序使用的服务集合。</param>
    public ApplicationCreationOptions(IServiceCollection services)
    {
        Services = services;
        Configuration = new ConfigurationBuilderOptions();
    }

    /// <summary>
    /// 获取服务集合。
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 获取配置。
    /// </summary>
    public ConfigurationBuilderOptions Configuration { get; }
}
