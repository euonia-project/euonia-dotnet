using MongoDB.Bson.Serialization;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// Mongo 模型键构建器。
/// </summary>
public class ModelKeyBuilder
{
    /// <summary>
    /// 使用键名与键类型初始化模型键构建器。
    /// </summary>
    /// <param name="key">键对应的字段名称。</param>
    /// <param name="type">键的 CLR 类型；为 <c>null</c> 时由映射过程推断。</param>
    internal ModelKeyBuilder(string key, Type? type)
    {
        Key = key;
        Type = type;
    }

    /// <summary>
    /// 获取键对应的字段名称。
    /// </summary>
    internal string Key { get; }

    /// <summary>
    /// 获取键的 CLR 类型；为 <c>null</c> 时表示需要在映射阶段推断。
    /// </summary>
    internal Type? Type { get; }

    /// <summary>
    /// 获取已配置的 Id 生成器；未配置时为 <c>null</c>。
    /// </summary>
    internal IIdGenerator? IdGenerator { get; private set; }

    /// <summary>
    /// 获取是否在插入时自动生成 Id，默认为 <c>true</c>。
    /// </summary>
    internal bool AutoGenerateId { get; private set; } = true;

    /// <summary>
    /// 设置一个值，指示是否自动生成 Id。
    /// </summary>
    /// <param name="value">指示是否在插入时自动生成 Id。</param>
    /// <returns>当前构建器实例，以支持链式调用。</returns>
    public ModelKeyBuilder GenerateOnInsert(bool value)
    {
        AutoGenerateId = value;
        return this;
    }

    /// <summary>
    /// 为当前属性设置值生成器。
    /// </summary>
    /// <typeparam name="TGenerator">实现 <see cref="IIdGenerator"/> 的 Id 生成器类型，需具有公共无参构造函数。</typeparam>
    /// <returns>当前构建器实例，以支持链式调用。</returns>
    public ModelKeyBuilder UseValueGenerator<TGenerator>()
        where TGenerator : class, IIdGenerator, new()
    {
        IdGenerator = Singleton<TGenerator>.Get(() => new());
        return this;
    }
}