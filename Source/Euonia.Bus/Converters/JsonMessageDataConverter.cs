namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 使用 <see cref="IMessageSerializer"/> 将消息数据流反序列化为 <typeparamref name="T"/> 类型对象的转换器。
/// </summary>
/// <typeparam name="T">转换后的目标类型。</typeparam>
public class JsonMessageDataConverter<T> : IMessageDataConverter<T>
{
	private readonly IMessageSerializer _serializer;

	/// <summary>
	/// 使用指定的序列化器创建 <see cref="JsonMessageDataConverter{T}"/> 的新实例。
	/// </summary>
	/// <param name="serializer">用于反序列化消息数据的序列化器。</param>
	public JsonMessageDataConverter(IMessageSerializer serializer)
	{
		_serializer = serializer;
	}

	/// <inheritdoc/>
	public Task<T> ConvertAsync(Stream stream, CancellationToken cancellationToken)
	{
		return _serializer.DeserializeAsync<T>(stream, cancellationToken);
	}
}