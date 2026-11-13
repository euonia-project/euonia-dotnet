namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 接收方接口，用于声明对特定消息类型的注册。
/// </summary>
/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
public interface IRecipient<in TMessage>
	where TMessage : class
{
	/// <summary>
	/// 接收给定的 <typeparamref name="TMessage"/> 消息实例。
	/// </summary>
	/// <param name="pack">正在接收的消息。</param>
	void Receive(TMessage pack);
}