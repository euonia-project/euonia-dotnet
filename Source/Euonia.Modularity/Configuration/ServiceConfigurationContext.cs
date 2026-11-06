using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 服务注册阶段的上下文，用于在模块配置服务时共享服务集合与命名对象。
/// </summary>
public class ServiceConfigurationContext
{
    /// <summary>
    /// 获取模块用于注册服务的服务集合。
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 获取可在模块之间共享命名对象的字典。
    /// </summary>
    public IDictionary<string, object> Items { get; }

    /// <summary>
    /// 获取/设置任意命名对象，这些对象可在服务注册阶段存储，并在模块之间共享。
    ///
    /// 这是 <see cref="Items"/> 字典的快捷用法。
    /// 如果给定的键未在 <see cref="Items"/> 字典中找到，则返回 null。
    /// </summary>
    /// <param name="key">要获取或设置的项名称。</param>
    /// <returns>与键对应的对象；键不存在时为 <see langword="null"/>。</returns>
    public object? this[string key]
    {
        get => Items.GetOrDefault(key);
        set => Items[key] = value!;
    }

    /// <summary>
    /// 初始化 <see cref="ServiceConfigurationContext"/> 的新实例。
    /// </summary>
    /// <param name="services">模块用于注册服务的服务集合，不能为 <see langword="null"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 为 <see langword="null"/>。</exception>
    public ServiceConfigurationContext(IServiceCollection services)
    {
        Services = Check.EnsureNotNull(services, nameof(services));
        Items = new Dictionary<string, object>();
    }
}