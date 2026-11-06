using MongoDB.Bson.Serialization;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The mongo model key builder.
/// </summary>
public class ModelKeyBuilder
{
    internal ModelKeyBuilder(string key, Type? type)
    {
        Key = key;
        Type = type;
    }

    internal string Key { get; }

    internal Type? Type { get; }

    internal IIdGenerator? IdGenerator { get; private set; }

    internal bool AutoGenerateId { get; private set; } = true;

    /// <summary>
    /// Sets a value indicating whether to automatically generate id.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public ModelKeyBuilder GenerateOnInsert(bool value)
    {
        AutoGenerateId = value;
        return this;
    }

    /// <summary>
    /// Sets the value generator for current property.
    /// </summary>
    /// <typeparam name="TGenerator"></typeparam>
    /// <returns></returns>
    public ModelKeyBuilder UseValueGenerator<TGenerator>()
        where TGenerator : class, IIdGenerator, new()
    {
        IdGenerator = Singleton<TGenerator>.Get(() => new());
        return this;
    }
}