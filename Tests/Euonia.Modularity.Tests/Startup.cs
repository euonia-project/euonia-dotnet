using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Modularity.Tests;

/// <summary>
/// 测试项目的启动配置。
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required by the test host.")]
public class Startup
{
    /// <summary>
    /// 配置测试宿主。
    /// </summary>
    /// <param name="hostBuilder">待配置的宿主构建器。</param>
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((_, _) => { });
    }

    /// <summary>
    /// 注册测试宿主所需的服务。
    /// </summary>
    /// <param name="services">依赖注入容器中的服务集合。</param>
    /// <param name="hostBuilderContext">当前宿主构建上下文。</param>
    public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    {
    }
}
