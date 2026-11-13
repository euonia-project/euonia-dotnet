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
        ModuleSources = new List<IModuleSource>();
    }

    /// <summary>
    /// 获取服务集合。
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 获取配置。
    /// </summary>
    public ConfigurationBuilderOptions Configuration { get; }

    /// <summary>
    /// 获取额外模块来源集合。
    /// </summary>
    /// <remarks>
    /// 启动模块依赖图之外的模块可以通过此集合加载，例如插件模块或运行时动态注册的模块。
    /// </remarks>
    public IList<IModuleSource> ModuleSources { get; }

    /// <summary>
    /// 添加额外模块来源。
    /// </summary>
    /// <param name="moduleSource">模块来源。</param>
    /// <returns>当前选项实例，便于链式调用。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="moduleSource"/> 为 <see langword="null"/> 时抛出。</exception>
    public ApplicationCreationOptions AddModuleSource(IModuleSource moduleSource)
    {
        ModuleSources.Add(Check.EnsureNotNull(moduleSource, nameof(moduleSource)));
        return this;
    }

    /// <summary>
    /// 添加额外模块类型。
    /// </summary>
    /// <param name="moduleTypes">要加载的模块类型。</param>
    /// <returns>当前选项实例，便于链式调用。</returns>
    public ApplicationCreationOptions AddModuleSource(params Type[] moduleTypes)
    {
        if (moduleTypes is { Length: > 0 })
        {
            ModuleSources.Add(new DelegateModuleSource(moduleTypes));
        }

        return this;
    }

    /// <summary>
    /// 从文件夹加载额外模块。
    /// </summary>
    /// <param name="path">程序集所在文件夹路径。</param>
    /// <param name="searchOption">搜索选项，默认只搜索当前文件夹。</param>
    /// <returns>当前选项实例，便于链式调用。</returns>
    public ApplicationCreationOptions AddModuleFolder(string path, System.IO.SearchOption searchOption = System.IO.SearchOption.TopDirectoryOnly)
    {
        return AddModuleSource(new FolderModuleSource(path, searchOption));
    }

    /// <summary>
    /// 从程序集文件加载额外模块。
    /// </summary>
    /// <param name="assemblyFiles">程序集文件路径。</param>
    /// <returns>当前选项实例，便于链式调用。</returns>
    public ApplicationCreationOptions AddModuleFiles(params string[] assemblyFiles)
    {
        return AddModuleSource(new FileModuleSource(assemblyFiles));
    }
}
