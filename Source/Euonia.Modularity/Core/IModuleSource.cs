namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义额外模块类型的来源，用于插件式模块或运行时动态注册模块。
/// </summary>
/// <remarks>
/// 启动模块依赖图中的模块会自动加载；该接口用于补充不在依赖图中、
/// 但需要在当前应用程序中加载的模块。
/// </remarks>
public interface IModuleSource
{
    /// <summary>
    /// 获取该来源提供的模块类型。
    /// </summary>
    /// <returns>模块类型集合。</returns>
    IEnumerable<Type> GetModules();
}

/// <summary>
/// 基于委托的 <see cref="IModuleSource"/> 实现。
/// </summary>
public sealed class DelegateModuleSource : IModuleSource
{
    private readonly Func<IEnumerable<Type>> _factory;

    /// <summary>
    /// 初始化 <see cref="DelegateModuleSource"/> 类的新实例。
    /// </summary>
    /// <param name="factory">返回模块类型的工厂方法。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="factory"/> 为 <see langword="null"/> 时抛出。</exception>
    public DelegateModuleSource(Func<IEnumerable<Type>> factory)
    {
        _factory = Check.EnsureNotNull(factory, nameof(factory));
    }

    /// <summary>
    /// 初始化 <see cref="DelegateModuleSource"/> 类的新实例。
    /// </summary>
    /// <param name="moduleTypes">该来源提供的模块类型。</param>
    public DelegateModuleSource(params Type[] moduleTypes)
        : this(() => moduleTypes ?? Array.Empty<Type>())
    {
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetModules()
    {
        return _factory();
    }
}
