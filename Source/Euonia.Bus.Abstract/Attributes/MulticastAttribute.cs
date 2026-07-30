namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示标记的类是多播消息
/// </summary>
/// <remarks>
///	此Attribute用于标记消息类为多播消息，表示该消息可以被多个接收者接收和处理。使用此特性可以在消息总线中实现多播通信模式，允许同一条消息被多个订阅者同时接收和处理。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MulticastAttribute : Attribute
{
}