namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// <see cref="MessengerExtensions"/> 的响应式扩展：把消息信使的订阅包装为 <see cref="IObservable{T}"/>。
/// </summary>
partial class MessengerExtensions
{
	/// <summary>
	/// 创建一个 <see cref="IObservable{T}"/> 实例，可用于在消息信使广播给定类型的消息时收到通知。
	/// </summary>
	/// <typeparam name="TMessage">要通过生成的 <see cref="IObservable{T}"/> 实例接收通知的消息类型。</typeparam>
	/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
	/// <returns>一个 <see cref="IObservable{T}"/> 实例，用于接收 <typeparamref name="TMessage"/> 消息被广播时的通知。</returns>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 为 <see langword="null"/> 时引发。</exception>
	public static IObservable<TMessage> CreateObservable<TMessage>(this IMessenger messenger)
		where TMessage : class
	{
		ArgumentNullException.ThrowIfNull(messenger);

		return new Observable<TMessage>(messenger);
	}

	/// <summary>
	/// 创建一个 <see cref="IObservable{T}"/> 实例，可用于在消息信使广播给定类型的消息时收到通知。
	/// </summary>
	/// <typeparam name="TMessage">要通过生成的 <see cref="IObservable{T}"/> 实例接收通知的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识接收消息所使用通道的令牌类型。</typeparam>
	/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="token">用于确定要使用的接收通道的令牌。</param>
	/// <returns>一个 <see cref="IObservable{T}"/> 实例，用于接收 <typeparamref name="TMessage"/> 消息被广播时的通知。</returns>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时引发。</exception>
	public static IObservable<TMessage> CreateObservable<TMessage, TToken>(this IMessenger messenger, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentNullException.ThrowIfNull(messenger);
		ArgumentNullException.ThrowIfNull(token);

		return new Observable<TMessage, TToken>(messenger, token);
	}

	/// <summary>
	/// 用于给定消息类型的 <see cref="IObservable{T}"/> 实现。
	/// </summary>
	/// <typeparam name="TMessage">要侦听的消息类型。</typeparam>
	private sealed class Observable<TMessage> : IObservable<TMessage>
		where TMessage : class
	{
		/// <summary>
		/// 用于注册接收方的 <see cref="IMessenger"/> 实例。
		/// </summary>
		private readonly IMessenger _messenger;

		/// <summary>
		/// 使用给定参数创建新的 <see cref="Observable{TMessage}"/> 实例。
		/// </summary>
		/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
		public Observable(IMessenger messenger)
		{
			_messenger = messenger;
		}

		/// <inheritdoc/>
		public IDisposable Subscribe(IObserver<TMessage> observer)
		{
			return new Recipient(_messenger, observer);
		}

		/// <summary>
		/// 用于 <see cref="Observable{TMessage}"/> 的 <see cref="IRecipient{TMessage}"/> 实现。
		/// </summary>
		private sealed class Recipient : IRecipient<TMessage>, IDisposable
		{
			/// <summary>
			/// 用于注册接收方的 <see cref="IMessenger"/> 实例。
			/// </summary>
			private readonly IMessenger _messenger;

			/// <summary>
			/// 当前正在使用的目标 <see cref="IObserver{T}"/> 实例。
			/// </summary>
			private readonly IObserver<TMessage> _observer;

			/// <summary>
			/// 使用指定参数创建新的 <see cref="Recipient"/> 实例。
			/// </summary>
			/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
			/// <param name="observer">用于创建接收方的 <see cref="IObserver{T}"/> 实例。</param>
			public Recipient(IMessenger messenger, IObserver<TMessage> observer)
			{
				_messenger = messenger;
				_observer = observer;

				messenger.Register(this);
			}

			/// <inheritdoc/>
			public void Receive(TMessage pack)
			{
				_observer.OnNext(pack);
			}

			/// <inheritdoc/>
			public void Dispose()
			{
				_messenger.Unregister<TMessage>(this);
			}
		}
	}

	/// <summary>
	/// 用于给定消息和令牌类型对的 <see cref="IObservable{T}"/> 实现。
	/// </summary>
	/// <typeparam name="TMessage">要侦听的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识接收消息所使用通道的令牌类型。</typeparam>
	private sealed class Observable<TMessage, TToken> : IObservable<TMessage>
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		/// <summary>
		/// 用于注册接收方的 <see cref="IMessenger"/> 实例。
		/// </summary>
		private readonly IMessenger _messenger;

		/// <summary>
		/// 用于确定要使用的接收通道的令牌。
		/// </summary>
		private readonly TToken _token;

		/// <summary>
		/// 使用给定参数创建新的 <see cref="Observable{TMessage, TToken}"/> 实例。
		/// </summary>
		/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
		/// <param name="token">用于确定要使用的接收通道的令牌。</param>
		public Observable(IMessenger messenger, TToken token)
		{
			_messenger = messenger;
			_token = token;
		}

		/// <inheritdoc/>
		public IDisposable Subscribe(IObserver<TMessage> observer)
		{
			return new Recipient(_messenger, observer, _token);
		}

		/// <summary>
		/// 用于 <see cref="Observable{TMessage, TToken}"/> 的 <see cref="IRecipient{TMessage}"/> 实现。
		/// </summary>
		private sealed class Recipient : IRecipient<TMessage>, IDisposable
		{
			/// <summary>
			/// 用于注册接收方的 <see cref="IMessenger"/> 实例。
			/// </summary>
			private readonly IMessenger _messenger;

			/// <summary>
			/// 当前正在使用的目标 <see cref="IObserver{T}"/> 实例。
			/// </summary>
			private readonly IObserver<TMessage> _observer;

			/// <summary>
			/// 用于确定要使用的接收通道的令牌。
			/// </summary>
			private readonly TToken _token;

			/// <summary>
			/// 使用指定参数创建新的 <see cref="Recipient"/> 实例。
			/// </summary>
			/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
			/// <param name="observer">用于创建接收方的 <see cref="IObserver{T}"/> 实例。</param>
			/// <param name="token">用于确定要使用的接收通道的令牌。</param>
			public Recipient(IMessenger messenger, IObserver<TMessage> observer, TToken token)
			{
				_messenger = messenger;
				_observer = observer;
				_token = token;

				messenger.Register(this, token);
			}

			/// <inheritdoc/>
			public void Receive(TMessage pack)
			{
				_observer.OnNext(pack);
			}

			/// <inheritdoc/>
			public void Dispose()
			{
				_messenger.Unregister<TMessage, TToken>(this, _token);
			}
		}
	}
}