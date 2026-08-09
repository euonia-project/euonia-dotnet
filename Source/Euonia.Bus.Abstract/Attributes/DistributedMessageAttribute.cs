namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 指示消息可被分发到分布式总线的标记特性。
/// 将此特性应用于消息类型以指定该消息可以通过分布式传输器进行发送或发布。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DistributedMessageAttribute : Attribute
{
}