using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// RabbitMQ 消息接收器的基类。
/// </summary>
public abstract class RabbitMqRecipient : DisposableObject
{
	/// <summary>
	/// 当消息被接收时触发。
	/// </summary>
	public event EventHandler<MessageReceivedEventArgs> MessageReceived;

	/// <summary>
	/// 当消息被确认（ACK）时触发。
	/// </summary>
	public event EventHandler<MessageAcknowledgedEventArgs> MessageAcknowledged;

	/// <summary>
	/// 初始化 <see cref="RabbitMqRecipient"/> 类的新实例。
	/// </summary>
	/// <param name="factory">用于建立和管理 RabbitMQ 连接的持久连接工厂。</param>
	/// <param name="options">包装在 <see cref="IOptions{T}"/> 中的 <see cref="RabbitMqBusOptions"/> 配置。</param>
	protected RabbitMqRecipient(IPersistentConnection factory, IOptions<RabbitMqBusOptions> options)
	{
		Options = options.Value;
		Connection = factory;
	}

	/// <summary>
	/// 获取用于与 RabbitMQ 进行通信的持久连接。
	/// </summary>
	protected IPersistentConnection Connection { get; }

	/// <summary>
	/// 获取 RabbitMQ 消息总线的配置选项。
	/// </summary>
	protected virtual RabbitMqBusOptions Options { get; }

	/// <summary>
	/// 处理接收到的消息。
	/// 由子类实现具体的消息处理逻辑。
	/// </summary>
	/// <param name="channel">消息所在的通道名称。</param>
	/// <param name="message">接收到的消息对象。</param>
	/// <param name="context">消息上下文，包含请求上下文等附加信息。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步消息处理操作的任务。</returns>
	protected abstract Task HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default);

	// protected virtual void AcknowledgeMessage(ulong deliveryTag)
	// {
	// 	Channel.BasicAck(deliveryTag, false);
	// }

	/// <summary>
	/// 启动接收器，开始监听指定通道的消息。
	/// </summary>
	/// <param name="channel">要监听的通道名称。</param>
	internal abstract Task StartAsync(string channel);

	/// <summary>
	/// 处理来自 RabbitMQ 的原始消息投递事件。
	/// 负责将 <see cref="BasicDeliverEventArgs"/> 转换为内部消息格式并触发处理流程。
	/// </summary>
	/// <param name="sender">事件发送方。</param>
	/// <param name="args">包含投递消息数据的 RabbitMQ 事件参数。</param>
	protected abstract Task HandleMessageReceivedAsync(object sender, BasicDeliverEventArgs args);

	/// <summary>
	/// 触发 <see cref="MessageAcknowledged"/> 事件。
	/// </summary>
	/// <param name="args">包含确认信息的消息确认事件参数。</param>
	protected virtual void OnMessageAcknowledged(MessageAcknowledgedEventArgs args)
	{
		MessageAcknowledged?.Invoke(this, args);
	}

	/// <summary>
	/// 触发 <see cref="MessageReceived"/> 事件。
	/// </summary>
	/// <param name="args">包含接收信息的消息接收事件参数。</param>
	protected virtual void OnMessageReceived(MessageReceivedEventArgs args)
	{
		MessageReceived?.Invoke(this, args);
	}

	/// <summary>
	/// 将消息对象序列化为字节数组。
	/// 使用 JSON 格式进行序列化，null 消息返回空字节数组。
	/// </summary>
	/// <param name="message">要序列化的消息对象。</param>
	/// <returns>序列化后的 UTF-8 字节数组；如果 <paramref name="message"/> 为 null，则返回空数组。</returns>
	protected virtual byte[] SerializeMessage(object message)
	{
		if (message == null)
		{
			return Array.Empty<byte>();
		}

		var json = JsonConvert.SerializeObject(message, Constants.SerializerSettings);
		return Encoding.UTF8.GetBytes(json);
	}

	/// <summary>
	/// 将字节数组反序列化为指定类型的消息信封。
	/// </summary>
	/// <param name="message">包含序列化消息的字节数组。</param>
	/// <param name="messageType">消息的运行时类型。</param>
	/// <returns>反序列化后的 <see cref="IMessageEnvelope"/> 实例。</returns>
	protected virtual IMessageEnvelope DeserializeMessage(byte[] message, Type messageType)
	{
		var type = typeof(IMessageEnvelope<>).MakeGenericType(messageType);
		var json = Encoding.UTF8.GetString(message);
		return JsonConvert.DeserializeObject(json, type, Constants.SerializerSettings) as IMessageEnvelope;
	}

	/// <summary>
	/// 从消息头字典中获取指定键的值。
	/// 支持字符串和字节数组类型的值。
	/// </summary>
	/// <param name="header">消息头键值对字典，可能为 null。</param>
	/// <param name="key">要查找的键名。</param>
	/// <returns>找到的字符串值；如果头字典为 null 或未找到指定键，则返回 <see cref="string.Empty"/>。</returns>
	protected virtual string GetHeaderValue(IDictionary<string, object> header, string key)
	{
		if (header == null)
		{
			return string.Empty;
		}

		if (header.TryGetValue(key, out var value))
		{
			return value switch
			{
				null => string.Empty,
				string @string => @string,
				byte[] bytes => Encoding.UTF8.GetString(bytes),
				_ => value.ToString()
			};
		}

		return string.Empty;
	}
}