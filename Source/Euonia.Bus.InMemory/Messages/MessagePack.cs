namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 定义用于传输的消息包。
/// </summary>
public sealed class MessagePack
{
	/// <summary>
	/// 初始化 <see cref="MessagePack"/> 类的新实例。
	/// </summary>
	/// <param name="message">要传递的消息信封，封装消息元数据与负载。</param>
	/// <param name="context">与该消息关联的消息上下文，用于跟踪处理状态并触发相应事件。</param>
	public MessagePack(IMessageEnvelope message, MessageContext context)
	{
		Message = message;
		Context = context;
	}

	/// <summary>
	/// 获取消息信封，其中包含消息元数据与负载。
	/// </summary>
	public IMessageEnvelope Message { get; }

	/// <summary>
	/// 获取消息上下文。
	/// </summary>
	public MessageContext Context { get; }

	/// <summary>
	/// 获取或设置用于中止消息处理的取消令牌。
	/// </summary>
	public CancellationToken Aborted { get; set; }
}