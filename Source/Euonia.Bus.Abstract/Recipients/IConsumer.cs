namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义队列消费者的标识接口，继承自 <see cref="IRecipient"/>，用于标记接收单播消息的消费端。
/// </summary>
public interface IConsumer : IRecipient
{
}