using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The mongo model profile.
/// </summary>
public class ModelProfile
{
    private ModelKeyBuilder? _keyBuilder;
    internal string? KeyName => _keyBuilder?.Key;

    internal Type? KeyType => _keyBuilder?.Type;

    internal string? CollectionName { get; private set; }

    internal bool AutoMapProperty { get; private set; } = true;

    /// <summary>
    /// 是否自动生成Id
    /// </summary>
    internal bool AutoGenerateId => _keyBuilder?.AutoGenerateId ?? true;

    internal IIdGenerator? IdGenerator => _keyBuilder?.IdGenerator;

    internal bool? BypassDocumentValidation { get; set; }

    /// <summary>
    /// Sets the model key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public ModelKeyBuilder HasKey(string key, Type? type = null)
    {
        _keyBuilder = new ModelKeyBuilder(key, type);
        return _keyBuilder;
    }

    /// <summary>
    /// Sets the collection name for current model.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public ModelProfile ToCollection(string name)
    {
        CollectionName = name;
        return this;
    }

    /// <summary>
    /// Sets a value indicating whether to automatically map properties.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public ModelProfile AutoMap(bool value)
    {
        AutoMapProperty = value;
        return this;
    }

    /// <summary>
    /// Sets a value indicating whether to bypass document validation.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
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

    internal IReadOnlyDictionary<string, ModelPropertyBuilder> Properties => _properties;

    internal Action<BsonClassMap<TModel>>? MapAction { get; private set; }

    /// <summary>
    /// Sets the model key selector.
    /// </summary>
    /// <param name="selector"></param>
    /// <returns></returns>
    public ModelKeyBuilder HasKey(Expression<Func<TModel, object>> selector)
    {
        var property = Reflect.GetProperty(selector);
        return HasKey(property.Name, property.PropertyType);
    }

    /// <summary>
    /// Sets the model property selector.
    /// </summary>
    /// <param name="selector"></param>
    /// <returns></returns>
    public ModelPropertyBuilder HasProperty(Expression<Func<TModel, object>> selector)
    {
        var property = Reflect.GetProperty(selector);
        var builder = new ModelPropertyBuilder(property.Name, property.PropertyType);
        _properties.Add(property.Name, builder);
        return builder;
    }

    /// <summary>
    /// Sets the model map action.
    /// </summary>
    /// <param name="map"></param>
    public void Map(Action<BsonClassMap<TModel>> map)
    {
        MapAction = map;
    }
}