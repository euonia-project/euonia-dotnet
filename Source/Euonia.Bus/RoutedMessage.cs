using System.Runtime.Serialization;
using System.Security.Claims;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 抽象路由消息基类，封装消息的基本标识、元数据和时间戳等信息。
/// </summary>
[Serializable]
public abstract class RoutedMessage
{
	/// <summary>
	/// 初始化 <see cref="RoutedMessage"/> 类的新实例。
	/// </summary>
	protected RoutedMessage()
	{
	}

	/// <summary>
	/// 消息类型键，用于在元数据中存储消息类型的程序集限定名。
	/// </summary>
	protected const string MessageTypeKey = "$nerosoft.euonia:message.type";

	/// <summary>
	/// 获取或设置消息标识符。
	/// </summary>
	[DataMember]
	public virtual string MessageId { get; set; } = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();

	/// <summary>
	/// 获取或设置关联标识符。
	/// </summary>
	[DataMember]
	public virtual string CorrelationId { get; set; } = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();

	/// <summary>
	/// 获取或设置会话标识符。
	/// </summary>
	[DataMember]
	public virtual string ConversationId { get; set; } = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();

	/// <summary>
	/// 获取或设置请求追踪标识符。
	/// </summary>
	[DataMember]
	public virtual string RequestTraceId { get; set; }

	/// <summary>
	/// 获取或设置消息发送的目标通道。
	/// </summary>
	[DataMember]
	public virtual string Channel { get; set; }

	/// <summary>
	/// 获取或设置消息的授权信息。
	/// </summary>
	[DataMember]
	public virtual string Authorization { get; set; }

	/// <summary>
	/// 获取或设置消息发生的时间戳（Unix 毫秒）。
	/// </summary>
	[DataMember]
	public virtual long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

	/// <summary>
	/// 获取包含消息元数据信息的 <see cref="MessageMetadata"/> 实例。
	/// </summary>
	[DataMember]
	public virtual MessageMetadata Metadata { get; set; } = new();

	/// <summary>
	/// 获取或设置当前请求的用户主体。
	/// </summary>
	[DataMember]
	public ClaimsPrincipal User { get; set; }

	/// <summary>
	/// 获取消息的 .NET CLR 程序集限定名。
	/// </summary>
	/// <returns>消息的程序集限定名。</returns>
	public virtual string GetTypeName() => Metadata[MessageTypeKey] as string;

	/// <summary>
	/// 返回当前实例的字符串表示形式。
	/// </summary>
	/// <returns>表示当前实例的字符串。</returns>
	public override string ToString() => $"{MessageId}:{{GetTypeName()}}";
}

/// <summary>
/// 泛型路由消息，继承自 <see cref="RoutedMessage"/> 并实现 <see cref="IMessageEnvelope{T}"/>，携带具体的消息负载。
/// </summary>
/// <typeparam name="T">消息负载的类型。</typeparam>
[Serializable]
public class RoutedMessage<T> : RoutedMessage, IMessageEnvelope<T>
	where T : class
{
	/// <summary>
	/// 使用指定的负载和通道初始化 <see cref="RoutedMessage{T}"/> 类的新实例。
	/// </summary>
	/// <param name="payload">消息负载数据。</param>
	/// <param name="channel">消息通道名称。</param>
	public RoutedMessage(T payload, string channel)
	{
		Payload = payload;
		Channel = channel;
	}

	/// <summary>
	/// 获取消息负载的类型名称。
	/// </summary>
	public string TypeName => GetTypeName();

	object IMessageEnvelope.Payload => Payload;

	/// <inheritdoc cref="Payload"/>
	private T _payload;

	/// <summary>
	/// 获取或设置消息的负载内容。
	/// 设置负载时会自动更新元数据中的消息类型信息。
	/// </summary>
	[DataMember]
	public T Payload
	{
		get => _payload;
		set
		{
			_payload = value;
			if (value != null)
			{
				Metadata[MessageTypeKey] = value.GetType().GetFullNameWithAssemblyName();
			}
		}
	}
}

/// <summary>
/// 带有请求-响应语义的泛型路由消息，继承自 <see cref="RoutedMessage{T}"/>。
/// </summary>
/// <typeparam name="TData">消息负载的类型。</typeparam>
/// <typeparam name="TResponse">响应的类型。</typeparam>
[Serializable]
public class RoutedMessage<TData, TResponse> : RoutedMessage<TData>
	where TData : class
{
	/// <summary>
	/// 使用指定的负载和通道初始化 <see cref="RoutedMessage{TData, TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="payload">消息负载数据。</param>
	/// <param name="channel">消息通道名称。</param>
	public RoutedMessage(TData payload, string channel)
		: base(payload, channel)
	{
	}
}