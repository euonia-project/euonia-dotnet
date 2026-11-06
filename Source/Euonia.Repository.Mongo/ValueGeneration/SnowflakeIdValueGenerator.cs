using MongoDB.Bson.Serialization;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 用于生成雪花 Id 的类。
/// </summary>
public class SnowflakeIdValueGenerator : IIdGenerator, IValueGenerator<long>
{
	/// <inheritdoc />
	public long Generate()
    {
        var id = ObjectId.NewSnowflake();
        return id;
    }

	/// <inheritdoc />
	public object GenerateId(object container, object document)
    {
        return Generate();
    }

	/// <inheritdoc />
	public bool IsEmpty(object id)
    {
        return id == null || (long)id == 0;
    }

    object IValueGenerator.Generate()
    {
        return Generate();
    }
}
