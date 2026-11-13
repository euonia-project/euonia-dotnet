namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 用于表示收到消息时要调用的操作的 <see langword="delegate"/>。
/// 接收方作为输入参数传入，以便消息注册避免创建闭包：如果需要调用接收方上的实例方法，
/// 只需将接收方转换为正确的类型，然后从该实例访问本地方法即可。
/// </summary>
/// <typeparam name="TRecipient">消息接收方的类型。</typeparam>
/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
/// <param name="recipient">正在接收消息的接收方。</param>
/// <param name="message">正在接收的消息。</param>
public delegate void MessageHandler<in TRecipient, in TMessage>(TRecipient recipient, TMessage message)
	where TRecipient : class
	where TMessage : class;