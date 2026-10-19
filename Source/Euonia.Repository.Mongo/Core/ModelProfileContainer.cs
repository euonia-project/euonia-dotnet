using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Repository.Mongo;

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

    public static ModelProfileContainer GetInstance(Type contextType, Action<ModelBuilder> onModelCreating)
    {
        return _instances.GetOrAdd(
            contextType,
            _ => new Lazy<ModelProfileContainer>(() => new ModelProfileContainer(onModelCreating))).Value;
    }

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