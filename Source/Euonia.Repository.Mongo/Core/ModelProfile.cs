using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// Mongo 模型配置。
/// </summary>
public class ModelProfile
{
    private ModelKeyBuilder? _keyBuilder;
    /// <summary>
    /// 获取模型键的字段名称；未配置键时为 <c>null</c>。
    /// </summary>
    internal string? KeyName => _keyBuilder?.Key;

    /// <summary>
    /// 获取模型键的 CLR 类型；未配置或需推断时为 <c>null</c>。
    /// </summary>
    internal Type? KeyType => _keyBuilder?.Type;

    /// <summary>
    /// 获取模型映射到的集合名称；未显式指定时为 <c>null</c>，此时使用实体类型名称。
    /// </summary>
    internal string? CollectionName { get; private set; }

    /// <summary>
    /// 获取是否自动映射未显式配置的属性，默认为 <c>true</c>。
    /// </summary>
    internal bool AutoMapProperty { get; private set; } = true;

    /// <summary>
    /// 获取是否在插入时自动生成 Id，未配置键时为 <c>true</c>。
    /// </summary>
    internal bool AutoGenerateId => _keyBuilder?.AutoGenerateId ?? true;

    /// <summary>
    /// 获取已配置的 Id 生成器；未配置时为 <c>null</c>。
    /// </summary>
    internal IIdGenerator? IdGenerator => _keyBuilder?.IdGenerator;

    /// <summary>
    /// 获取或设置是否在写入时跳过集合的文档验证；为 <c>null</c> 时沿用集合默认行为。
    /// </summary>
    internal bool? BypassDocumentValidation { get; set; }

    /// <summary>
    /// 设置模型键。
    /// </summary>
    /// <param name="key">键对应的字段名称。</param>
    /// <param name="type">键的 CLR 类型；为 <c>null</c> 时根据模型中同名字段的类型推断。</param>
    /// <returns>用于继续配置键生成行为的 <see cref="ModelKeyBuilder"/>。</returns>
    public ModelKeyBuilder HasKey(string key, Type? type = null)
    {
        _keyBuilder = new ModelKeyBuilder(key, type);
        return _keyBuilder;
    }

    /// <summary>
    /// 设置当前模型的集合名称。
    /// </summary>
    /// <param name="name">模型映射到的集合名称。</param>
    /// <returns>当前实例，以便链式调用。</returns>
    public ModelProfile ToCollection(string name)
    {
        CollectionName = name;
        return this;
    }

    /// <summary>
    /// 设置一个值，指示是否自动映射属性。
    /// </summary>
    /// <param name="value">若为 <c>true</c>，则自动映射未显式配置的属性；否则仅映射已显式声明的属性。</param>
    /// <returns>当前实例，以便链式调用。</returns>
    public ModelProfile AutoMap(bool value)
    {
        AutoMapProperty = value;
        return this;
    }

    /// <summary>
    /// 设置一个值，指示是否跳过文档验证。
    /// </summary>
    /// <param name="value">若为 <c>true</c>，则写入时跳过集合的文档验证规则；为 <c>false</c> 时执行验证。</param>
    /// <returns>当前实例，以便链式调用。</returns>
    public ModelProfile BypassValidation(bool value)
    {
        BypassDocumentValidation = value;
        return this;
    }
}

/// <inheritdoc />
public class ModelProfile<TModel> : ModelProfile
{
    private readonly Dictionary<string, ModelPropertyBuilder> _properties = new();

    /// <summary>
    /// 获取按属性名索引的显式属性配置集合。
    /// </summary>
    internal IReadOnlyDictionary<string, ModelPropertyBuilder> Properties => _properties;

    /// <summary>
    /// 获取自定义的类映射操作；为 <c>null</c> 时使用框架默认映射逻辑。
    /// </summary>
    internal Action<BsonClassMap<TModel>>? MapAction { get; private set; }

    /// <summary>
    /// 设置模型键选择器。
    /// </summary>
    /// <param name="selector">选择键属性的表达式，该属性名与类型将用于配置模型键。</param>
    /// <returns>用于继续配置键生成行为的 <see cref="ModelKeyBuilder"/>。</returns>
    public ModelKeyBuilder HasKey(Expression<Func<TModel, object>> selector)
    {
        var property = Reflect.GetProperty(selector);
        return HasKey(property.Name, property.PropertyType);
    }

    /// <summary>
    /// 设置模型属性选择器。
    /// </summary>
    /// <param name="selector">选择要显式配置的属性的表达式。</param>
    /// <returns>用于继续配置该属性映射方式的 <see cref="ModelPropertyBuilder"/>。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="selector"/> 选中的属性已被配置过时引发。</exception>
    public ModelPropertyBuilder HasProperty(Expression<Func<TModel, object>> selector)
    {
        var property = Reflect.GetProperty(selector);
        var builder = new ModelPropertyBuilder(property.Name, property.PropertyType);
        _properties.Add(property.Name, builder);
        return builder;
    }

    /// <summary>
    /// 设置模型映射操作。
    /// </summary>
    /// <param name="map">应用于 <see cref="BsonClassMap{TModel}"/> 的映射操作。</param>
    /// <remarks>
    /// 设置后将替代框架的默认映射逻辑，此时 <see cref="ModelProfile.AutoMap"/> 等配置不再对属性映射生效。
    /// </remarks>
    public void Map(Action<BsonClassMap<TModel>> map)
    {
        MapAction = map;
    }
}