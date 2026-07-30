namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义主题订阅者的标识接口，继承自 <see cref="IRecipient"/>，用于标记接收多播消息的订阅端。
/// </summary>
/// <seealso cref="IRecipient" />
public interface ISubscriber : IRecipient
{
}