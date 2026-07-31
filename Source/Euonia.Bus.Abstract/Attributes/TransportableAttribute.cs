namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 标记一个消息类型为可通过传输器进行发送的抽象标记特性。
/// 将此特性应用于类以指示该消息类型可被序列化并通过消息传输器进行传输。
/// </summary>
/// <remarks>
/// 此Attribute是抽象类，不能直接使用，需要通过其派生类（如 <c>MulticastAttribute</c>、<c>UnicastAttribute</c>、<c>RequestAttribute</c> 等）来标记具体的消息类型。
/// 仅用于类级别标记，且不可被派生类继承。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class TransportableAttribute : Attribute
{
}