using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 保存单个 Mongo 数据上下文的模型配置（集合名、键、属性映射）。
/// </summary>
internal class ModelProfileContainer
{
    /// <summary>
    /// 按数据上下文类型缓存配置容器。
    /// </summary>
    /// <remarks>
    /// 不能使用进程级单例：否则只有第一个上下文类型的 <c>OnModelCreating</c> 会执行，
    /// 其余上下文声明的集合名、键与属性映射会被静默忽略。
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, Lazy<ModelProfileContainer>> _instances = new();

    private readonly ModelBuilder _builder = new();
    private readonly Dictionary<Type, ModelProfile> _mappings = new();

    private ModelProfileContainer(Action<ModelBuilder> onModelCreating)
    {
        _builder.OnConfigure = OnModelConfigure;
        onModelCreating(_builder);
    }

    /// <summary>
    /// 获取指定数据上下文类型对应的配置容器；首次访问时执行该上下文的 <c>OnModelCreating</c>。
    /// </summary>
    /// <param name="contextType">数据上下文的运行时类型。</param>
    /// <param name="onModelCreating">该上下文用于构建模型配置的委托。</param>
    /// <returns>与 <paramref name="contextType"/> 绑定的配置容器。</returns>
    public static ModelProfileContainer GetInstance(Type contextType, Action<ModelBuilder> onModelCreating)
    {
        return _instances.GetOrAdd(
            contextType,
            _ => new Lazy<ModelProfileContainer>(() => new ModelProfileContainer(onModelCreating))).Value;
    }

    /// <summary>
    /// 获取已注册的实体类型到模型配置的映射。
    /// </summary>
    public IReadOnlyDictionary<Type, ModelProfile> Mappings => _mappings;

    private void OnModelConfigure(Type type, ModelProfile profile)
    {
        _mappings[type] = profile;
    }

    internal ModelProfile GetProfile<T>()
    {
        return GetProfile(typeof(T));
    }

    internal ModelProfile GetProfile(Type type)
    {
        return _mappings.TryGetValue(type, out var profile) ? profile : new ModelProfile();
    }
}