using MongoDB.Bson.Serialization;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 用于生成顺序 GUID 的类。
/// </summary>
public class SequentialGuidValueGenerator : IIdGenerator, IValueGenerator<Guid>
{
	/// <inheritdoc />
	public Guid Generate()
    {
        return ObjectId.NewGuid(GuidType.SequentialAsString);
    }

	/// <inheritdoc />
	public object GenerateId(object container, object document)
    {
        return Generate();
    }

	/// <inheritdoc />
	public bool IsEmpty(object id)
    {
        return id == null || (Guid)id == Guid.Empty;
    }

    object IValueGenerator.Generate()
    {
        return Generate();
    }
}
