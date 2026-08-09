using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus.SystemTextJson;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 基于 System.Text.Json 的消息序列化器实现。
/// 提供消息对象与 JSON 之间的同步和异步序列化/反序列化功能。
/// </summary>
public class SystemTextJsonSerializer : IMessageSerializer
{
	private readonly JsonSerializerOptions _options;

	/// <summary>
	/// 初始化 <see cref="SystemTextJsonSerializer"/> 的新实例。
	/// </summary>
	/// <param name="options">包装在 <see cref="IOptions{T}"/> 中的 <see cref="MessageSerializerOptions"/> 配置。</param>
	public SystemTextJsonSerializer(IOptions<MessageSerializerOptions> options)
	{
		_options = new JsonSerializerOptions
		{
			DefaultIgnoreCondition = options.Value.IgnoreNullValues ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
			Converters =
			{
				new ClaimsPrincipalJsonConverter(),
				new ClaimsIdentityJsonConverter(),
				new ClaimJsonConverter()
			},
		};
		_options.ReferenceHandler = options.Value.ReferenceLoop switch
		{
			MessageSerializerOptions.ReferenceLoopStrategy.Ignore => ReferenceHandler.IgnoreCycles,
			MessageSerializerOptions.ReferenceLoopStrategy.Preserve => ReferenceHandler.Preserve,
			MessageSerializerOptions.ReferenceLoopStrategy.Serialize => null,
			_ => _options.ReferenceHandler
		};
	}

	/// <summary>
	/// 将对象异步序列化为 UTF-8 字节数组。
	/// </summary>
	/// <typeparam name="T">要序列化的对象类型。</typeparam>
	/// <param name="source">要序列化的对象。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>序列化后的 UTF-8 字节数组。</returns>
	public async Task<byte[]> SerializeAsync<T>(T source, CancellationToken cancellationToken = default)
	{
		await using var stream = new MemoryStream();
		await JsonSerializer.SerializeAsync(stream, source, _options, cancellationToken: cancellationToken);
		return stream.ToArray();
	}

	/// <summary>
	/// 从流中异步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="source">包含 JSON 数据的流。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>反序列化后的对象。</returns>
	public async Task<T> DeserializeAsync<T>(Stream source, CancellationToken cancellationToken = default)
	{
		return await JsonSerializer.DeserializeAsync<T>(source, _options, cancellationToken: cancellationToken);
	}

	/// <summary>
	/// 从字节数组中异步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="source">包含 JSON 数据的字节数组。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>反序列化后的对象。</returns>
	public async Task<T> DeserializeAsync<T>(byte[] source, CancellationToken cancellationToken = default)
	{
		await using var stream = new MemoryStream(source);
		return await DeserializeAsync<T>(stream, cancellationToken);
	}

	/// <summary>
	/// 从字节数组中同步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="source">包含 JSON 数据的字节数组。</param>
	/// <returns>反序列化后的对象。</returns>
	public T Deserialize<T>(byte[] source)
	{
		return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(source), _options);
	}

	/// <summary>
	/// 从 JSON 字符串中同步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="source">JSON 字符串。</param>
	/// <returns>反序列化后的对象。</returns>
	public T Deserialize<T>(string source)
	{
		return JsonSerializer.Deserialize<T>(source, _options);
	}

	/// <summary>
	/// 从流中同步反序列化为指定类型的对象。
	/// </summary>
	/// <typeparam name="T">目标反序列化类型。</typeparam>
	/// <param name="stream">包含 JSON 数据的流。</param>
	/// <returns>反序列化后的对象。</returns>
	public T Deserialize<T>(Stream stream)
	{
		return JsonSerializer.Deserialize<T>(stream, _options);
	}

	/// <summary>
	/// 将对象同步序列化为 JSON 字符串。
	/// </summary>
	/// <typeparam name="T">要序列化的对象类型。</typeparam>
	/// <param name="source">要序列化的对象。</param>
	/// <returns>序列化后的 JSON 字符串。</returns>
	public string Serialize<T>(T source)
	{
		return JsonSerializer.Serialize(source, _options);
	}

	/// <summary>
	/// 从 JSON 字符串中同步反序列化为指定类型的对象。
	/// </summary>
	/// <param name="source">JSON 字符串。</param>
	/// <param name="type">目标反序列化的运行时类型。</param>
	/// <returns>反序列化后的对象。</returns>
	public object Deserialize(string source, Type type)
	{
		return JsonSerializer.Deserialize(source, type, _options);
	}

	/// <summary>
	/// 从 JSON 字符串中反序列化为指定负载类型的消息信封。
	/// </summary>
	/// <param name="source">包含信封数据的 JSON 字符串。</param>
	/// <param name="payloadType">信封中消息负载的运行时类型。</param>
	/// <returns>反序列化后的 <see cref="IMessageEnvelope"/> 实例。</returns>
	public IMessageEnvelope DeserializeEnvelope(string source, Type payloadType)
	{
		var type = typeof(RoutedMessage<>).MakeGenericType(payloadType);
		return (IMessageEnvelope)JsonSerializer.Deserialize(source, type, _options);
	}
}