namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 用于在不同对象之间交换消息的类型接口。
/// 这有助于解耦应用中的不同模块，而无需持有被引用类型的强引用。
/// 还可以将消息发送到由令牌唯一标识的特定通道，并在应用的不同部分使用不同的消息信使。
/// 若要使用 <see cref="IMessenger"/> 的功能，首先定义一个消息类型，如下所示：
/// <code>
/// public sealed class LoginCompletedMessage { }
/// </code>
/// 然后，为该消息注册接收方：
/// <code>
/// Messenger.Default.Register&lt;MyRecipientType, LoginCompletedMessage&gt;(this, (r, m) =>
/// {
///     // Handle the message here...
/// });
/// </code>
/// 这里的消息处理程序是一个接受两个参数的 lambda 表达式：接收方和消息。
/// 这样做是为了避免在表达式捕获当前实例时产生闭包分配。使用接收方类型参数，
/// 是为了让处理程序可以直接访问接收方，而无需手动进行类型转换。这使代码更简洁、
/// 更可靠，因为所有检查都在编译时完成。如果处理程序定义在接收方所在的类型中，
/// 还可以直接访问私有成员。这使得消息处理程序可以是静态方法，从而让 C# 编译器
/// 执行多项额外的内存优化（例如缓存委托、避免不必要的内存分配）。
/// 最后，在需要时发送消息，如下所示：
/// <code>
/// Messenger.Default.Send&lt;LoginCompletedMessage&gt;();
/// </code>
/// 此外，如果当前作用域中存在签名正确的方法，还可以使用方法组语法指定
/// 接收消息时要调用的消息处理程序。这有助于将注册逻辑与处理逻辑分开。
/// 继续前面的示例，假设某个类具有以下方法：
/// <code>
/// private static void Receive(MyRecipientType recipient, LoginCompletedMessage message)
/// {
///     // Handle the message there
/// }
/// </code>
/// 然后就可以像下面这样用一行代码完成注册：
/// <code>
/// Messenger.Default.Register(this, Receive);
/// </code>
/// </summary>
public interface IMessenger
{
	/// <summary>
	/// 检查指定的接收方是否已针对某条消息注册。
	/// </summary>
	/// <typeparam name="TMessage">要检查指定接收方注册情况的消息类型。</typeparam>
	/// <typeparam name="TToken">要检查的通道的令牌类型。</typeparam>
	/// <param name="recipient">要检查注册情况的目标接收方。</param>
	/// <param name="token">用于标识要检查的目标通道的令牌。</param>
	/// <returns><paramref name="recipient"/> 是否已针对指定消息注册。</returns>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="recipient"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时引发。</exception>
	bool IsRegistered<TMessage, TToken>(object recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>;

	/// <summary>
	/// 为指定类型的消息注册接收方。
	/// </summary>
	/// <typeparam name="TRecipient">消息接收方的类型。</typeparam>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	/// <typeparam name="TToken">用于选择要接收的消息的令牌类型。</typeparam>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <param name="token">用于确定要使用的接收通道的令牌。</param>
	/// <param name="handler">接收消息时要调用的 <see cref="MessageHandler{TRecipient,TMessage}"/>。</param>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="recipient"/>、<paramref name="token"/> 或 <paramref name="handler"/> 为 <see langword="null"/> 时引发。</exception>
	/// <exception cref="InvalidOperationException">尝试重复注册同一条消息时引发。</exception>
	void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
		where TRecipient : class
		where TMessage : class
		where TToken : IEquatable<TToken>;

	/// <summary>
	/// 从所有已注册的消息中注销某个接收方。
	/// </summary>
	/// <param name="recipient">要注销的接收方。</param>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="recipient"/> 为 <see langword="null"/> 时引发。</exception>
	/// <remarks>
	/// 此方法将在所有通道中注销目标接收方。
	/// 可通过此方法便捷地释放对目标接收方的全部引用。
	/// 如果该接收方没有已注册的处理程序，此方法不执行任何操作。
	/// </remarks>
	void UnregisterAll(object recipient);

	/// <summary>
	/// 从指定通道上的所有消息中注销某个接收方。
	/// </summary>
	/// <typeparam name="TToken">用于标识要从哪个通道注销的令牌类型。</typeparam>
	/// <param name="recipient">要注销的接收方。</param>
	/// <param name="token">用于标识要注销哪些处理程序的令牌。</param>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="recipient"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时引发。</exception>
	/// <remarks>如果该接收方没有已注册的处理程序，此方法不执行任何操作。</remarks>
	void UnregisterAll<TToken>(object recipient, TToken token)
		where TToken : IEquatable<TToken>;

	/// <summary>
	/// 从指定类型的消息中注销某个接收方。
	/// </summary>
	/// <typeparam name="TMessage">要停止接收的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识要从哪个通道注销的令牌类型。</typeparam>
	/// <param name="recipient">要注销的接收方。</param>
	/// <param name="token">用于标识要注销哪些处理程序的令牌。</param>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="recipient"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时引发。</exception>
	/// <remarks>如果该接收方没有已注册的处理程序，此方法不执行任何操作。</remarks>
	void Unregister<TMessage, TToken>(object recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>;

	/// <summary>
	/// 将指定类型的消息发送给所有已注册的接收方。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识发送消息时使用哪个通道的令牌类型。</typeparam>
	/// <param name="message">要发送的消息。</param>
	/// <param name="token">指示要使用哪个通道的令牌。</param>
	/// <returns>已发送的消息（即 <paramref name="message"/>）。</returns>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="message"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时引发。</exception>
	TMessage Send<TMessage, TToken>(TMessage message, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>;

	/// <summary>
	/// 对当前消息信使执行清理。
	/// 调用此方法不会注销任何当前已注册的接收方，
	/// 它可用于执行清理操作，例如精简消息信使实现的内部数据结构。
	/// </summary>
	void Cleanup();

	/// <summary>
	/// 重置 <see cref="IMessenger"/> 实例，并注销所有现有接收方。
	/// </summary>
	void Reset();
}