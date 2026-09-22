using System.ComponentModel;
using MongoDB.Bson.Serialization;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The class used to serialize a <see cref="ObjectId"/> to a <see cref="string"/>.
/// </summary>
public class ObjectIdSerializer : IBsonSerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectIdSerializer"/> class.
    /// </summary>
    /// <param name="valueType"></param>
    public ObjectIdSerializer(Type valueType)
    {
        ValueType = valueType;
    }

    /// <inheritdoc />
    public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var id = context.Reader.ReadObjectId().ToString();
        return TypeDescriptor.GetConverter(ValueType).ConvertFrom(id);
    }

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        context.Writer.WriteObjectId(ToObjectId(value));
    }

    /// <summary>
    /// 将键值转换为 BSON <see cref="ObjectId"/>。
    /// </summary>
    /// <param name="value">键值。</param>
    /// <returns>对应的 <see cref="ObjectId"/>。</returns>
    /// <exception cref="NotSupportedException">当键值无法表示为 <see cref="ObjectId"/> 时抛出。</exception>
    /// <remarks>
    /// 与 <see cref="Deserialize"/> 互为逆操作：反序列化走 <c>ObjectId → 24 位十六进制字符串 → ValueType</c>，
    /// 因此可序列化的键类型必须能表示为 <see cref="ObjectId"/>（即 <see cref="ObjectId"/>、其字符串形式，
    /// 或 12 字节原始值）。
    /// <para>
    /// 此前该方法忽略传入的 <paramref name="value"/>，无条件写入 <c>MongoDB.Bson.ObjectId.Empty</c>，
    /// 导致同类型的每个文档 <c>_id</c> 都是全零——第二次插入即主键冲突，按 id 的读取也失去意义。
    /// 对无法表示的键类型现在会显式失败，而不是静默写入错误的标识符。
    /// </para>
    /// </remarks>
    private static MongoDB.Bson.ObjectId ToObjectId(object value)
    {
        switch (value)
        {
            case null:
                return MongoDB.Bson.ObjectId.Empty;
            case MongoDB.Bson.ObjectId id:
                return id;
            case string text when MongoDB.Bson.ObjectId.TryParse(text, out var parsed):
                return parsed;
            case byte[] bytes when bytes.Length == 12:
                return new MongoDB.Bson.ObjectId(bytes);
        }

        throw new NotSupportedException($"The key value '{value}' of type '{value.GetType()}' cannot be represented as a MongoDB ObjectId. Use MongoDB.Bson.ObjectId, a 24-character hexadecimal string, or a 12-byte array, or configure a different key type for this model.");
    }

    /// <inheritdoc />
    public Type ValueType { get; }
}

/// <summary>
/// The class used to serialize a <see cref="ObjectId"/> to a <see cref="string"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectIdSerializer<T> : ObjectIdSerializer, IBsonSerializer<T>
{
	/// <inheritdoc />
	public ObjectIdSerializer()
        : base(typeof(T))
    {
    }

	/// <inheritdoc />
	public new T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return (T)base.Deserialize(context, args);
    }

	/// <inheritdoc />
	public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        base.Serialize(context, args, value);
    }
}