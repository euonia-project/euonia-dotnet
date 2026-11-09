using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 为 <see cref="IMessenger"/> 接口提供参考实现的类。
/// </summary>
/// <remarks>
/// <para>
/// 此 <see cref="IMessenger"/> 实现使用弱引用来跟踪已注册的
/// 接收方，因此无需在不再需要时手动取消注册它们。
/// </para>
/// <para>
/// 当执行完整 GC 回收时，<see cref="WeakReferenceMessenger"/> 类型会自动执行内部裁剪，
/// 因此无需手动调用 <see cref="Cleanup"/> 来确保内部数据结构平均而言尽可能精简紧凑。
/// </para>
/// </remarks>
public sealed class WeakReferenceMessenger : IMessenger
{
	// 此消息信使使用以下逻辑将存储的实例关联在一起：
	// --------------------------------------------------------------------------------------------------------
	//                          ITypeDictionary<TToken, MessageHandlerDispatcher?> mapping
	//                                        /                   /             /
	//                   ___(EquatableType.TToken)___/                   /             /         ___(若 EquatableType.TToken 为 Unit)
	//                  /_________(EquatableType.TMessage)______________/             /         /
	//                 /                                    _________________/___MessageHandlerDispatcher?
	//                /                                    /                         \
	// ITypeDictionary<EquatableType, ConditionalWeakTable<object, object?>> recipientsMap;       \___(若使用 IRecipient<TMessage> 则为 null)
	// --------------------------------------------------------------------------------------------------------
	// 与强引用变体一样，每一对消息类型和令牌类型都用作接收方映射中的键。
	// 在此情况下，字典中的值是 ConditionalWeakTable2<,> 实例，它们通过依赖句柄
	// 将每个已注册接收方与当前已注册处理程序的映射关联起来。这确保只要关联的接收方
	// 仍然存活，处理程序就会保持存活（因此用户无需手动指示某个处理程序是否应保持存活，
	// 以防它创建闭包）。
	// 每个条件表中的值可以是 ITypeDictionary<TToken, MessageHandlerDispatcher> 或 object。
	// 第一种情况用于使用任何非默认 Unit 类型的令牌类型，因为此时每个接收方可能有多个
	// 需要分别跟踪的处理程序。为了在类型参数未知的上下文中调用所有处理程序，处理程序
	// 以 MessageHandlerDispatcher 实例的形式存储。这里有两种可能的情况：给定实例是
	// MessageHandlerDispatcher.For<TRecipient, TMessage> 类型，或者为 null。第一种是默认情况：
	// 当通过 MessageHandler<TRecipient, TToken> 完成订阅时，该委托会被包装在此类的实例中，
	// 以便它能够在内部跟踪所使用的泛型上下文，从而在回调执行时能够检索到它。如果订阅
	// 直接建立在实现 IRecipient<TMessage 的接收方上，则调度器为 null，它仅充当标记。
	// 每当广播方法发现它时，就会直接在目标接收方上调用 IRecipient<TMessage.Receive，
	// 这样既避免了调度时的额外间接调用，也避免了为处理程序额外分配包装类型。最后，还有
	// 一种特殊情况，即通过 Unit 类型完成订阅，也就是在使用默认通道时。在这种情况下，
	// 每个接收方只存储单个 MessageHandlerDispatcher 实例，而不是整个字典，因为每个
	// 接收方最多只能有一个处理程序。

	/// <summary>
	/// 所有消息类型当前已注册接收方的映射。
	/// </summary>
	private readonly TypeDictionary<EquatableType, ConditionalWeakTable2<object, object>> _recipientsMap = new();

	/// <summary>
	/// 初始化 <see cref="WeakReferenceMessenger"/> 类的新实例。
	/// </summary>
	public WeakReferenceMessenger()
	{
		// GC 回调的代理函数。此函数必须是静态的，并以目标实例作为输入参数，
		// 以避免它被调用它的 Gen2GcCallback 对象所根引用。
		static void Gen2GcCallbackProxy(object target)
		{
			((WeakReferenceMessenger)target).CleanupWithNonBlockingLock();
		}

		// 注册一个自动 GC 回调以触发非阻塞清理。这确保当前消息信使实例被裁剪，
		// 且不留下不再使用的接收方映射。这是必要的（也就是说，需要某种形式的清理，
		// 无论是显式清理还是像本例这样的自动清理），因为 ConditionalWeakTable<TKey, TValue>
		// 实例会自行移除键值对——一旦某个键（即接收方）被回收，就会导致它们自己的键
		// （即映射到每对消息类型和令牌类型的每个条件表的 EquatableType 实例）可能仍保留在
		// 根映射结构中，但其中实际上已没有任何注册的接收方，这会在之后进行广播操作时
		// 枚举接收方带来不必要的开销。
		Gen2GcCallback.Register(Gen2GcCallbackProxy, this);
	}

	/// <summary>
	/// 获取默认的 <see cref="WeakReferenceMessenger"/> 实例。
	/// </summary>
	public static WeakReferenceMessenger Default { get; } = new();

	/// <inheritdoc/>
	public bool IsRegistered<TMessage, TToken>(object recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentNullException.ThrowIfNull(recipient);
		ArgumentNullException.ThrowIfNull(token);

		lock (_recipientsMap)
		{
			EquatableType equatableType = new(typeof(TMessage), typeof(TToken));

			// 获取与目标接收方关联的、针对当前令牌类型和消息类型对的条件表。
			// 如果存在，则检查是否有匹配的令牌。
			if (!_recipientsMap.TryGetValue(equatableType, out var table))
			{
				return false;
			}

			// Unit 令牌的特殊情况
			if (typeof(TToken) == typeof(Unit))
			{
				return table.TryGetValue(recipient, out _);
			}

			// 自定义令牌类型，因此每个接收方都有相关联的映射
			return
				table.TryGetValue(recipient, out var mapping) &&
				Unsafe.As<TypeDictionary<TToken, object>>(mapping!).ContainsKey(token);
		}
	}

	/// <inheritdoc/>
	public void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
		where TRecipient : class
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentNullException.ThrowIfNull(recipient);
		ArgumentNullException.ThrowIfNull(token);
		ArgumentNullException.ThrowIfNull(handler);

		Register<TMessage, TToken>(recipient, token, new MessageHandlerDispatcher.For<TRecipient, TMessage>(handler));
	}

	/// <summary>
	/// 为给定类型的消息注册接收方。
	/// </summary>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	/// <typeparam name="TToken">用于筛选要接收的消息的令牌类型。</typeparam>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <param name="token">用于确定要使用的接收通道的令牌。</param>
	/// <exception cref="InvalidOperationException">尝试重复注册同一消息时抛出。</exception>
	/// <remarks>
	/// 此方法是 <see cref="Register{TRecipient, TMessage, TToken}(TRecipient, TToken, MessageHandler{TRecipient, TMessage})"/>
	/// 的变体，专用于实现 <see cref="IRecipient{TMessage}"/> 的接收方。更多说明请参见此类型顶部的注释，
	/// 以及 <see cref="Send{TMessage, TToken}(TMessage, TToken)"/> 和 <see cref="MessageHandlerDispatcher"/> 类型中的注释。
	/// </remarks>
	internal void Register<TMessage, TToken>(IRecipient<TMessage> recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		Register<TMessage, TToken>(recipient, token, null);
	}

	/// <summary>
	/// 为给定类型的消息注册接收方。
	/// </summary>
	/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
	/// <typeparam name="TToken">用于筛选要接收的消息的令牌类型。</typeparam>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <param name="token">用于确定要使用的接收通道的令牌。</param>
	/// <param name="dispatcher">要注册的 <see cref="MessageHandlerDispatcher"/> 实例，或为 null。</param>
	/// <exception cref="InvalidOperationException">尝试重复注册同一消息时抛出。</exception>
	private void Register<TMessage, TToken>(object recipient, TToken token, MessageHandlerDispatcher? dispatcher)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		lock (_recipientsMap)
		{
			EquatableType equatableType = new(typeof(TMessage), typeof(TToken));

			// 获取类型参数对对应的条件表，如果不存在则创建它
			ref var mapping = ref _recipientsMap.GetOrAddValueRef(equatableType);

			mapping ??= new ConditionalWeakTable2<object, object>();

			// Unit 令牌的快速路径
			if (typeof(TToken) == typeof(Unit))
			{
				if (!mapping.TryAdd(recipient, dispatcher))
				{
					ThrowInvalidOperationExceptionForDuplicateRegistration();
				}
			}
			else
			{
				// 获取或创建目标接收方的处理程序字典
				var map = Unsafe.As<TypeDictionary<TToken, object>>(mapping.GetValue(recipient, static _ => new TypeDictionary<TToken, object>())!);

				// 添加新的注册项
				ref var registeredHandler = ref map.GetOrAddValueRef(token);

				if (registeredHandler is not null)
				{
					ThrowInvalidOperationExceptionForDuplicateRegistration();
				}

				// 存储输入的处理程序
				registeredHandler = dispatcher;
			}
		}
	}

	/// <inheritdoc/>
	public void UnregisterAll(object recipient)
	{
		ArgumentNullException.ThrowIfNull(recipient);

		lock (_recipientsMap)
		{
			var enumerator = _recipientsMap.GetEnumerator();

			// 遍历所有现有的条件表，并移除所有以目标接收方为键的表。此处不执行清理，
			// 因为那是下面定义的另一个方法的职责。
			while (enumerator.MoveNext())
			{
				_ = enumerator.GetValue().Remove(recipient);
			}
		}
	}

	/// <inheritdoc/>
	public void UnregisterAll<TToken>(object recipient, TToken token)
		where TToken : IEquatable<TToken>
	{
		ArgumentNullException.ThrowIfNull(recipient);
		ArgumentNullException.ThrowIfNull(token);

		// 此方法从不会在 unit 类型下被调用。更多细节请参见
		// StrongReferenceMessenger 中对应方法内的注释。
		if (typeof(TToken) == typeof(Unit))
		{
			throw new NotImplementedException();
		}

		lock (_recipientsMap)
		{
			var enumerator = _recipientsMap.GetEnumerator();

			// 与上面相同，区别在于此次只遍历以匹配的令牌类型为键的条件表，
			// 并且仅尝试移除具有匹配令牌的处理程序（如果存在）。
			while (enumerator.MoveNext())
			{
				if (enumerator.GetKey().Token == typeof(TToken))
				{
					if (enumerator.GetValue().TryGetValue(recipient, out var mapping))
					{
						_ = Unsafe.As<TypeDictionary<TToken, object>>(mapping!).TryRemove(token);
					}
				}
			}
		}
	}

	/// <inheritdoc/>
	public void Unregister<TMessage, TToken>(object recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentNullException.ThrowIfNull(recipient);
		ArgumentNullException.ThrowIfNull(token);

		lock (_recipientsMap)
		{
			EquatableType equatableType = new(typeof(TMessage), typeof(TToken));

			// 获取消息类型和令牌类型组合对应的目标映射表，
			// 并移除具有匹配令牌的处理程序（整个映射，如存在）。
			if (_recipientsMap.TryGetValue(equatableType, out var value))
			{
				if (typeof(TToken) == typeof(Unit))
				{
					_ = value.Remove(recipient);
				}
				else if (value.TryGetValue(recipient, out var mapping))
				{
					_ = Unsafe.As<TypeDictionary<TToken, object>>(mapping!).TryRemove(token);
				}
			}
		}
	}

	/// <inheritdoc/>
	public TMessage Send<TMessage, TToken>(TMessage message, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentNullException.ThrowIfNull(message);
		ArgumentNullException.ThrowIfNull(token);

		ArrayPoolBufferWriter<object> bufferWriter;
		var i = 0;

		lock (_recipientsMap)
		{
			EquatableType equatableType = new(typeof(TMessage), typeof(TToken));

			// 尝试获取目标表
			if (!_recipientsMap.TryGetValue(equatableType, out var table))
			{
				return message;
			}

			bufferWriter = ArrayPoolBufferWriter<object>.Create();

			// 我们需要所有待处理接收方和待调用处理程序的本地临时副本，
			// 以避免在我们持有锁期间处理程序从消息中取消注册所引发的问题。
			// 为此，我们只需遍历正在使用的条件表，以枚举与本次调用的泛型参数
			// 对应的令牌类型和消息类型对的所有现有接收方，然后跟踪具有匹配令牌的
			// 处理程序及其对应的接收方。
			using var enumerator = table.GetEnumerator();

			while (enumerator.MoveNext())
			{
				if (typeof(TToken) == typeof(Unit))
				{
					bufferWriter.Add(enumerator.GetValue());
					bufferWriter.Add(enumerator.GetKey());
					i++;
				}
				else
				{
					var map = Unsafe.As<TypeDictionary<TToken, object>>(enumerator.GetValue()!);

					if (map.TryGetValue(token, out var handler))
					{
						bufferWriter.Add(handler);
						bufferWriter.Add(enumerator.GetKey());
						i++;
					}
				}
			}
		}

		try
		{
			SendAll(bufferWriter.Span, i, message);
		}
		finally
		{
			bufferWriter.Dispose();
		}

		return message;
	}

	/// <summary>
	/// 实现 <see cref="Send{TMessage, TToken}(TMessage, TToken)"/> 的广播逻辑。
	/// </summary>
	/// <typeparam name="TMessage">要广播的消息类型。</typeparam>
	/// <param name="pairs">按“处理程序、接收方”成对排列的跨度。</param>
	/// <param name="i">要广播的接收方数量。</param>
	/// <param name="message">要广播的消息。</param>
	/// <remarks>
	/// 此方法不是局部函数，以避免因 <c>TToken</c> 可能是值类型而触发多次编译，
	/// 后者会因具化泛型而产生特化代码。这是为绕过 Roslyn 的一项限制所必需的，
	/// 该限制会导致局部函数中不必要的类型参数在合成方法中未被丢弃。此外，
	/// 将此循环保留在 EH 块（<see langword="try"/> 块）之外有助于获得略好的代码生成。
	/// </remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void SendAll<TMessage>(ReadOnlySpan<object> pairs, int i, TMessage message)
		where TMessage : class
	{
		// 此 Slice 调用会为下面的循环执行边界检查，以防 i 以某种方式出错。
		// 实现的其余部分依赖手动完成的边界检查消除和循环强度削减
		// （这在广播期间带来约 20% 的加速），因为 JIT 无法识别此模式。
		// 跳过下面的检查是经过证明安全的优化：该切片恰好有 2 * i 个元素
		// （由于此切片操作），且每次循环迭代处理一对元素。当初始引用到达末尾时
		// 循环结束，并且在每次迭代结束时递增 2。目标为 span，显然意味着长度是固定的。
		var slice = pairs[..(2 * i)];

		ref object sliceStart = ref MemoryMarshal.GetReference(slice);
		ref object sliceEnd = ref Unsafe.Add(ref sliceStart, slice.Length);

		while (Unsafe.IsAddressLessThan(ref sliceStart, ref sliceEnd))
		{
			var handler = sliceStart;
			var recipient = Unsafe.Add(ref sliceStart, 1)!;

			// 这里我们需要区分两种可能的情况：接收方是通过 IRecipient<TMessage> 接口注册的，
			// 还是通过自定义处理程序注册的。在第一种情况下，消息信使中存储的处理程序仅为 null，
			// 因此我们可以检查这一点并分支到快速路径，直接在接收方上调用 IRecipient<TMessage>。
			// 否则，我们将使用标准的双重分派方式。此检查特别方便，因为我们只需检查是否为 null
			// 就能确定使用了哪种注册类型，而无需在消息信使中存储任何额外信息。这将生成如下代码，
			// 其优点是同样紧凑且无需使用任何额外寄存器：
			// =============================
			// L0000: test rcx, rcx
			// L0003: jne short L0040
			// =============================
			// 这非常快。首先要进行此条件检查的原因是，我们正在执行手动的（基于 null 的）
			// 受保护去虚化：如果处理程序是标记类型而非实际处理程序，那么我们知道接收方实现了
			// IRecipient<TMessage>，因此只需将其强制转换并直接调用即可。这避免了在注册时
			// 存储代理回调，也跳过了一次间接调用（调用委托，再由委托调用实际方法）。
			// 补充说明：此模式确保下面的两次强制转换不会真正别名化不兼容的引用类型
			// （也就是说，如果它们是安全转换，则两者都会成功），从而使代码无需依赖未定义行为
			// 即可正确运行（即我们并未对委托进行别名化）。
			if (handler is null)
			{
				Unsafe.As<IRecipient<TMessage>>(recipient).Receive(message);
			}
			else
			{
				Unsafe.As<MessageHandlerDispatcher>(handler).Invoke(recipient, message);
			}

			sliceStart = ref Unsafe.Add(ref sliceStart, 2);
		}
	}

	/// <inheritdoc/>
	public void Cleanup()
	{
		lock (_recipientsMap)
		{
			CleanupWithoutLock();
		}
	}

	/// <inheritdoc/>
	public void Reset()
	{
		lock (_recipientsMap)
		{
			_recipientsMap.Clear();
		}
	}

	/// <summary>
	/// 在不锁定当前实例的情况下执行清理。此方法必须在
	/// 已获取对 <see cref="_recipientsMap"/> 的锁时调用。
	/// </summary>
	private void CleanupWithNonBlockingLock()
	{
		object lockObject = _recipientsMap;
		var lockTaken = false;

		try
		{
			Monitor.TryEnter(lockObject, ref lockTaken);

			if (lockTaken)
			{
				CleanupWithoutLock();
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(lockObject);
			}
		}
	}

	/// <summary>
	/// 在不锁定当前实例的情况下执行清理。此方法必须在
	/// 已获取对 <see cref="_recipientsMap"/> 的锁时调用。
	/// </summary>
	private void CleanupWithoutLock()
	{
		using var type = ArrayPoolBufferWriter<EquatableType>.Create();
		using var emptyRecipients = ArrayPoolBufferWriter<object>.Create();

		var type2Enumerator = _recipientsMap.GetEnumerator();

		// 首先，我们遍历所有当前已注册的令牌类型和消息类型对。
		// 这些代表至少有一个已注册处理程序的所有泛型参数组合，
		// 但接收方已被回收的组合除外。
		while (type2Enumerator.MoveNext())
		{
			emptyRecipients.Reset();

			var hasAtLeastOneHandler = false;

			if (type2Enumerator.GetKey().Token == typeof(Unit))
			{
				// 当令牌类型为 unit 时，不可能存在没有处理程序的已注册接收方，
				// 因为当唯一的处理程序被取消订阅时，接收方也会立即被移除。
				// 因此，我们需要检查该消息至少存在一个接收方。
				using var recipientsEnumerator = type2Enumerator.GetValue().GetEnumerator();

				while (recipientsEnumerator.MoveNext())
				{
					hasAtLeastOneHandler = true;

					break;
				}
			}
			else
			{
				// 遍历当前存活的接收方，查找其中已无剩余处理程序的接收方。我们记录
				// 找到的接收方，以便在循环外移除它们（枚举期间无法修改）。
				using (var recipientsEnumerator = type2Enumerator.GetValue().GetEnumerator())
				{
					while (recipientsEnumerator.MoveNext())
					{
						if (Unsafe.As<ITypeDictionary>(recipientsEnumerator.GetValue()!).Count == 0)
						{
							emptyRecipients.Add(recipientsEnumerator.GetKey());
						}
						else
						{
							hasAtLeastOneHandler = true;
						}
					}
				}

				// 移除仍然存活但已无处理程序的接收方的处理程序映射
				foreach (var recipient in emptyRecipients.Span)
				{
					_ = type2Enumerator.GetValue().Remove(recipient);
				}
			}

			// 跟踪已无接收方或处理程序剩余的类型组合
			if (!hasAtLeastOneHandler)
			{
				type.Add(type2Enumerator.GetKey());
			}
		}

		// 移除所有已无处理程序剩余的映射
		foreach (var key in type.Span)
		{
			_ = _recipientsMap.TryRemove(key);
		}
	}

	/// <summary>
	/// 在尝试添加重复的处理程序时抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	private static void ThrowInvalidOperationExceptionForDuplicateRegistration()
	{
		throw new InvalidOperationException(Resources.IDS_RECIPIENT_ALREADY_SUBSCRIBED);
	}
}