namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息序列化器接口。
/// 定义消息对象的序列化与反序列化约定，以及消息信封的反序列化能力。
/// </summary>
public interface IMessageSerializer
{
	/// <summary>
	/// 将对象异步序列化为 UTF-8 字节数组。
	/// </summary>
	/// <typeparam name="T">要序列化的对象类型。</typeparam>
	/// <param name="source">要序列化的对象。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>序列化后的 UTF-8 字节数组。</returns>
	Task<byte[]> SerializeAsync<T>(T source, CancellationToken cancellationToken = default);

	/// <summary>
	/// 从流中异步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="source">包含 JSON 数据的流。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>反序列化后的对象。</returns>
	Task<T> DeserializeAsync<T>(Stream source, CancellationToken cancellationToken = default);

	/// <summary>
	/// 从字节数组中异步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="source">包含 JSON 数据的字节数组。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>反序列化后的对象。</returns>
	Task<T> DeserializeAsync<T>(byte[] source, CancellationToken cancellationToken = default);

	/// <summary>
	/// 将指定类型的消息对象序列化为字符串。
	/// </summary>
	/// <typeparam name="T">消息的类型。</typeparam>
	/// <param name="source">要序列化的消息实例。</param>
	/// <returns>序列化后的 JSON 字符串。</returns>
	string Serialize<T>(T source);

	/// <summary>
	/// 从字节数组中同步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="source">包含 JSON 数据的字节数组。</param>
	/// <returns>反序列化后的对象。</returns>
	T Deserialize<T>(byte[] source);

	/// <summary>
	/// 从流中同步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="source">包含 JSON 数据的流。</param>
	/// <returns>反序列化后的对象。</returns>
	T Deserialize<T>(Stream source);

	/// <summary>
	/// 将字符串反序列化为指定类型的消息对象。
	/// </summary>
	/// <typeparam name="T">目标消息类型。</typeparam>
	/// <param name="source">要反序列化的字符串。</param>
	/// <returns>反序列化后的消息对象。</returns>
	T Deserialize<T>(string source);

	/// <summary>
	/// 从 JSON 字符串中同步反序列化为指定运行时类型的对象。
	/// </summary>
	/// <param name="source">JSON 字符串。</param>
	/// <param name="type">目标反序列化的运行时类型。</param>
	/// <returns>反序列化后的对象。</returns>
	object Deserialize(string source, Type type);

	/// <summary>
	/// 将字符串反序列化为消息信封。
	/// </summary>
	/// <param name="source">包含信封数据的字符串。</param>
	/// <param name="payloadType">信封中负载的类型。</param>
	/// <returns>反序列化后的 <see cref="IMessageEnvelope"/> 实例。</returns>
	IMessageEnvelope DeserializeEnvelope(string source, Type payloadType);
}