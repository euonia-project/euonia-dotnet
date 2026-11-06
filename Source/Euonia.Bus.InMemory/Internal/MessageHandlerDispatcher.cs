using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 调用给定 <see cref="MessageHandler{TRecipient, TMessage}"/> 回调的调度器类型。
/// </summary>
/// <remarks>
/// 当泛型参数未知时，该类型用于避免与 <see cref="Unsafe.As{T}(object)"/> 产生类型别名。
/// 此外，该类型是抽象类而非接口，以便在调用 <see cref="Invoke(object, object)"/> 时使用
/// 虚分派而不是接口存根分派，后者速度慢得多且间接层更多。
/// </remarks>
internal abstract class MessageHandlerDispatcher
{
	/// <summary>
	/// 使用指定的消息，在目标接收方上调用当前回调。
	/// </summary>
	/// <param name="recipient">消息的目标接收方。</param>
	/// <param name="message">正在广播的消息。</param>
	public abstract void Invoke(object recipient, object message);

	/// <summary>
	/// <see cref="MessageHandlerDispatcher"/> 的泛型版本。
	/// </summary>
	/// <typeparam name="TRecipient">消息接收方的类型。</typeparam>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	public sealed class For<TRecipient, TMessage> : MessageHandlerDispatcher
		where TRecipient : class
		where TMessage : class
	{
		/// <summary>
		/// 要调用的底层 <see cref="MessageHandler{TRecipient, TMessage}"/> 回调。
		/// </summary>
		private readonly MessageHandler<TRecipient, TMessage> _handler;

		/// <summary>
		/// 初始化 <see cref="MessageHandlerDispatcher.For{TRecipient,TMessage}"/> 类的新实例。
		/// </summary>
		/// <param name="handler">输入的 <see cref="MessageHandler{TRecipient, TMessage}"/> 实例。</param>
		public For(MessageHandler<TRecipient, TMessage> handler)
		{
			_handler = handler;
		}

		/// <inheritdoc/>
		public override void Invoke(object recipient, object message)
		{
			_handler(Unsafe.As<TRecipient>(recipient), Unsafe.As<TMessage>(message));
		}
	}
}