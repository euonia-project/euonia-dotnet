namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 指示消息仅通过本地总线（进程内）进行分发的标记特性。
/// 将此特性应用于消息类型以指定该消息只在当前进程内传递，不会通过分布式传输器发送。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LocalMessageAttribute : Attribute
{
}