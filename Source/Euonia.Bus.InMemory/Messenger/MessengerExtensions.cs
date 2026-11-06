using System.Reflection;
using System.Runtime.CompilerServices;
// ReSharper disable UnusedType.Local
// ReSharper disable UnusedMember.Local

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// <see cref="IMessenger"/> 类型的扩展方法。
/// </summary>
public static partial class MessengerExtensions
{
	/// <summary>
	/// 充当容器的类，用于加载与
	/// <see cref="Register{TMessage,TToken}(IMessenger,IRecipient{TMessage},TToken)"/> 方法关联的 <see cref="MethodInfo"/> 实例。
	/// 需要此类是为了避免在引用 <see cref="MessengerExtensions"/> 类型时立即执行静态构造函数中的初始化代码，
	/// 即使引用它只是为了使用那些实际上并不需要此 <see cref="MethodInfo"/> 实例的方法。
	/// 我们实际上利用此类型来利用运行时对静态构造函数的延迟加载。
	/// </summary>
	private static class MethodInfos
	{
		/// <summary>
		/// 与 <see cref="Register{TMessage,TToken}(IMessenger,IRecipient{TMessage},TToken)"/> 关联的 <see cref="MethodInfo"/> 实例。
		/// </summary>
		public static readonly MethodInfo RegisterIRecipient = new Action<IMessenger, IRecipient<object>, Unit>(Register).Method.GetGenericMethodDefinition();
	}

	/// <summary>
	/// <see cref="DiscoveredRecipients{TToken}"/> 的非泛型版本。
	/// </summary>
	private static class DiscoveredRecipients
	{
		/// <summary>
		/// 用于跟踪每个接收方预加载的注册操作的 <see cref="ConditionalWeakTable{TKey,TValue}"/> 实例。
		/// </summary>
		public static readonly ConditionalWeakTable<Type, Action<IMessenger, object>> RegistrationMethods = new();
	}

	/// <summary>
	/// 充当静态容器的类，用于为每个正在使用的 <typeparamref name="TToken"/> 类型关联一个
	/// <see cref="ConditionalWeakTable{TKey,TValue}"/> 实例。这样做是因为我们只能使用单一类型作为键，
	/// 但还需要跟踪每个接收方类型在不同通信通道（每个通道由令牌标识）中的关联关系。
	/// 由于令牌实际上是编译时参数，我们可以使用一个包装类让运行时为每个泛型类型实例化处理不同的实例。
	/// 这让我们只需关注被检查的接收方类型。
	/// </summary>
	/// <typeparam name="TToken">指示要使用哪个通道的令牌。</typeparam>
	private static class DiscoveredRecipients<TToken>
		where TToken : IEquatable<TToken>
	{
		/// <summary>
		/// 用于跟踪每个接收方预加载的注册操作的 <see cref="ConditionalWeakTable{TKey,TValue}"/> 实例。
		/// </summary>
		public static readonly ConditionalWeakTable<Type, Action<IMessenger, object, TToken>> RegistrationMethods = new();
	}

	/// <summary>
	/// 检查指定的接收方是否已针对某条消息注册。
	/// </summary>
	/// <typeparam name="TMessage">要检查指定接收方注册情况的消息类型。</typeparam>
	/// <param name="messenger">用于检查注册情况的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="recipient">要检查注册情况的目标接收方。</param>
	/// <returns><paramref name="recipient"/> 是否已针对指定消息注册。</returns>
	/// <remarks>此方法将使用默认通道检查所请求的注册。</remarks>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="recipient"/> 为 <see langword="null"/> 时引发。</exception>
	public static bool IsRegistered<TMessage>(this IMessenger messenger, object recipient)
		where TMessage : class
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.ThrowIfNull(recipient);

		return messenger.IsRegistered<TMessage, Unit>(recipient, default);
	}

	/// <summary>
	/// 为指定类型的消息注册接收方。
	/// </summary>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <exception cref="InvalidOperationException">尝试重复注册同一条消息时引发。</exception>
	/// <remarks>此方法将使用默认通道执行所请求的注册。</remarks>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="recipient"/> 为 <see langword="null"/> 时引发。</exception>
	public static void Register<TMessage>(this IMessenger messenger, IRecipient<TMessage> recipient)
		where TMessage : class
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.ThrowIfNull(recipient);

		if (messenger is WeakReferenceMessenger weakReferenceMessenger)
		{
			weakReferenceMessenger.Register<TMessage, Unit>(recipient, default);
		}
		else if (messenger is StrongReferenceMessenger strongReferenceMessenger)
		{
			strongReferenceMessenger.Register<TMessage, Unit>(recipient, default);
		}
		else
		{
			messenger.Register<IRecipient<TMessage>, TMessage, Unit>(recipient, default, static (r, m) => r.Receive(m));
		}
	}

	/// <summary>
	/// 为指定类型的消息注册接收方。
	/// </summary>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识接收消息时使用哪个通道的令牌类型。</typeparam>
	/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <param name="token">指示要使用哪个通道的令牌。</param>
	/// <exception cref="InvalidOperationException">尝试重复注册同一条消息时引发。</exception>
	/// <remarks>此方法将使用默认通道执行所请求的注册。</remarks>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时引发。</exception>
	public static void Register<TMessage, TToken>(this IMessenger messenger, IRecipient<TMessage> recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		if (messenger is WeakReferenceMessenger weakReferenceMessenger)
		{
			weakReferenceMessenger.Register(recipient, token);
		}
		else if (messenger is StrongReferenceMessenger strongReferenceMessenger)
		{
			strongReferenceMessenger.Register(recipient, token);
		}
		else
		{
			messenger.Register<IRecipient<TMessage>, TMessage, TToken>(recipient, token, static (r, m) => r.Receive(m));
		}
	}

	/// <summary>
	/// 为指定类型的消息注册接收方。
	/// </summary>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <param name="handler">接收消息时要调用的 <see cref="MessageHandler{TRecipient,TMessage}"/>。</param>
	/// <exception cref="InvalidOperationException">尝试重复注册同一条消息时引发。</exception>
	/// <remarks>此方法将使用默认通道执行所请求的注册。</remarks>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="handler"/> 为 <see langword="null"/> 时引发。</exception>
	public static void Register<TMessage>(this IMessenger messenger, object recipient, MessageHandler<object, TMessage> handler)
		where TMessage : class
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.ThrowIfNull(handler);

		messenger.Register(recipient, default(Unit), handler);
	}

	/// <summary>
	/// 为指定类型的消息注册接收方。
	/// </summary>
	/// <typeparam name="TRecipient">消息接收方的类型。</typeparam>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <param name="handler">接收消息时要调用的 <see cref="MessageHandler{TRecipient,TMessage}"/>。</param>
	/// <exception cref="InvalidOperationException">尝试重复注册同一条消息时引发。</exception>
	/// <remarks>此方法将使用默认通道执行所请求的注册。</remarks>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="handler"/> 为 <see langword="null"/> 时引发。</exception>
	public static void Register<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, MessageHandler<TRecipient, TMessage> handler)
		where TRecipient : class
		where TMessage : class
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.ThrowIfNull(handler);

		messenger.Register(recipient, default(Unit), handler);
	}

	/// <summary>
	/// 为指定类型的消息注册接收方。
	/// </summary>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	/// <typeparam name="TToken">用于选择要接收的消息的令牌类型。</typeparam>
	/// <param name="messenger">用于注册接收方的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <param name="token">用于确定要使用的接收通道的令牌。</param>
	/// <param name="handler">接收消息时要调用的 <see cref="MessageHandler{TRecipient,TMessage}"/>。</param>
	/// <exception cref="InvalidOperationException">尝试重复注册同一条消息时引发。</exception>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/>、<paramref name="recipient"/> 或 <paramref name="handler"/> 为 <see langword="null"/> 时引发。</exception>
	public static void Register<TMessage, TToken>(this IMessenger messenger, object recipient, TToken token, MessageHandler<object, TMessage> handler)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);
		ArgumentAssert.ThrowIfNull(handler);

		messenger.Register(recipient, token, handler);
	}

	/// <summary>
	/// 从指定类型的消息中注销某个接收方。
	/// </summary>
	/// <typeparam name="TMessage">要停止接收的消息类型。</typeparam>
	/// <param name="messenger">用于注销接收方的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="recipient">要注销的接收方。</param>
	/// <remarks>
	/// 此方法仅从默认通道中注销目标接收方。
	/// 如果该接收方没有已注册的处理程序，此方法不执行任何操作。
	/// </remarks>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="recipient"/> 为 <see langword="null"/> 时引发。</exception>
	public static void Unregister<TMessage>(this IMessenger messenger, object recipient)
		where TMessage : class
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.ThrowIfNull(recipient);

		messenger.Unregister<TMessage, Unit>(recipient, default);
	}

	/// <summary>
	/// 将指定类型的消息发送给所有已注册的接收方。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <param name="messenger">用于发送消息的 <see cref="IMessenger"/> 实例。</param>
	/// <returns>已发送的消息。</returns>
	/// <remarks>
	/// 当消息类型公开无参数构造函数时，此方法是 <see cref="Send{TMessage}(IMessenger,TMessage)"/> 的简写形式：
	/// 它会自动创建新的 <typeparamref name="TMessage"/> 实例，并将其发送给接收方。
	/// </remarks>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 为 <see langword="null"/> 时引发。</exception>
	public static TMessage Send<TMessage>(this IMessenger messenger)
		where TMessage : class, new()
	{
		ArgumentAssert.ThrowIfNull(messenger);

		return messenger.Send(new TMessage(), default(Unit));
	}

	/// <summary>
	/// 将指定类型的消息发送给所有已注册的接收方。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <param name="messenger">用于发送消息的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="message">要发送的消息。</param>
	/// <returns>已发送的消息（即 <paramref name="message"/>）。</returns>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="message"/> 为 <see langword="null"/> 时引发。</exception>
	public static TMessage Send<TMessage>(this IMessenger messenger, TMessage message)
		where TMessage : class
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.ThrowIfNull(message);

		return messenger.Send(message, default(Unit));
	}

	/// <summary>
	/// 将指定类型的消息发送给所有已注册的接收方。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识发送消息时使用哪个通道的令牌类型。</typeparam>
	/// <param name="messenger">用于发送消息的 <see cref="IMessenger"/> 实例。</param>
	/// <param name="token">指示要使用哪个通道的令牌。</param>
	/// <returns>已发送的消息。</returns>
	/// <remarks>
	/// 与 <see cref="Send{TMessage}(IMessenger)"/> 一样，此方法会自动创建新的 <typeparamref name="TMessage"/> 实例，
	/// 然后将其发送给相应的接收方。
	/// </remarks>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="messenger"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时引发。</exception>
	public static TMessage Send<TMessage, TToken>(this IMessenger messenger, TToken token)
		where TMessage : class, new()
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(messenger);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		return messenger.Send(new TMessage(), token);
	}
}