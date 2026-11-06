using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 为 <see cref="IMessenger"/> 接口提供参考实现的类。
/// </summary>
/// <remarks>
/// 此 <see cref="IMessenger"/> 实现使用强引用来跟踪已注册的
/// 接收方，因此当它们不再需要时必须手动注销。
/// </remarks>
public sealed class StrongReferenceMessenger : IMessenger
{
	// 此消息信使使用以下逻辑将存储的实例关联在一起：
	// --------------------------------------------------------------------------------------------------------
	//   ITypeDictionary<Recipient, HashSet<IMapping>> recipientsMap;
	//                   |                 \________________[*]ITypeDictionary<Recipient, ITypeDictionary<TToken>>
	//                   |                  \_______________[*]ITypeDictionary<Recipient, object?>       /
	//                   |                                           \_________/_________/___        / 
	//                   |\                       _(recipients registrations)_/         /    \      /
	//                   | \__________________   /    _____(channel registrations)_____/______\____/
	//                   |                    \ /    /      __________________________/        \
	//                   |                     /    /      /                                    \
	//                   |      ITypeDictionary<Recipient, object?> mapping = Mapping________________\
	//                   | __________________/    /                |         /                    \
	//                   |/                      /                 |        /                      \
	//    ITypeDictionary<Recipient, ITypeDictionary<TToken, object?>> mapping = Mapping<TToken>____________\
	//                                         /                  /       /  /
	//                   ___(EquatableType.TToken)____/                  /       /  /
	//                  /________________(EquatableType.TMessage)_______/_______/__/
	//                 /       ________________________________/
	//                /       /
	// ITypeDictionary<EquatableType, IMapping> typesMap;
	// --------------------------------------------------------------------------------------------------------
	// 每种 <TMessage, TToken> 组合都会产生一个具体的 Mapping 类型（若 TToken 为 Unit）或 Mapping<Token> 类型，
	// 它保存从已注册接收方到处理程序的引用。当请求默认通道时使用 Mapping，
	// 因为在这种情况下每个接收方、每种消息类型最多只会有一个处理程序。此时，
	// 每个接收方只跟踪消息调度器（存储为 object?，见下文说明），而不是使用一个将
	// 每个 TToken 值映射到该接收方对应调度器的字典。当使用自定义通道时，
	// 调度器存储在 <TToken, object?> 字典中，这样每个接收方对于给定的令牌，
	// 每种消息类型最多可以有一个已注册的处理程序。注意，已注册的调度器只以 object 引用的形式存储，
	// 因为它们要么为 null，要么是 MessageHandlerDispatcher.For<TRecipient, TMessage> 实例。
	//
	// 第一种情况发生在处理程序通过 IRecipient<TMessage> 实例注册时，第二种情况则
	// 用于包装输入的 MessageHandler<TRecipient, TMessage> 实例。调用时，
	// MessageHandlerDispatcher.For<TRecipient, TMessage> 实例只会被强制转换为 MessageHandlerDispatcher。
	// 这允许用户在各自已注册的接收方上保留类型信息，而无需在处理程序内部手动将
	// 每个接收方强制转换为正确的类型（此外，此处使用双重分派避免了为委托类型起别名的需要）。
	// 由于消息信使类型本身的工作方式，类型转换保证会被遵守——因为已注册的处理程序
	// 总是在其各自的接收方上调用。
	//
	// 每个映射都存储在类型映射中，后者将每一对具体类型关联到其映射实例。映射实例
	// 以 IMapping 项的形式暴露，因为每个实例都是针对 TMessage 和 TToken 泛型类型参数
	// 不同组合（对于默认通道则仅针对 TMessage）的封闭类型。每个现存的接收方也会存储在
	// 主接收方映射中，同时存储该接收方所有现存处理程序（字典）的集合（涵盖所有消息类型和令牌类型，如有）。
	//
	// 只要接收方在每种消息/令牌类型组合的任一现存映射中至少有一个已注册的处理程序，
	// 它就会存储在主映射中。共享映射用于访问给定接收方的所有已注册处理程序的集合，
	// 无需事先知道注册时使用的消息或令牌类型，也无需使用反射。这与类型映射中使用的
	// 方法相同，因为我们也把保存的项以 IMapping 值的形式暴露。
	//
	// 注意，存储在每个接收方关联集合中的每个映射也间接实现了
	// ITypeDictionary<Recipient, Token> 或 ITypeDictionary<Recipient>，其令牌类型为该接收方
	// 当前使用的类型（若使用默认通道则没有令牌）。这使得可以获取给定令牌类型、任意消息类型、
	// 每个接收方对应的已注册处理程序的类型封闭映射，同样无需使用反射。此共享映射用于
	// 从给定接收方注销消息，可以无条件注销、按消息类型注销、按令牌注销，
	// 或按特定的消息类型与令牌值组合注销。

	/// <summary>
	/// 当前已注册接收方的集合，并链接到其关联的消息接收器。
	/// </summary>
	/// <remarks>
	/// 此集合用于从 <see cref="UnregisterAll"/> 及此类型中的其他方法无反射地访问
	/// 所有现存的已注册接收方，从而可以移除所有现存处理程序，而无需为
	/// 映射处理程序的各种字典容器动态创建泛型类型。
	/// </remarks>
	private readonly TypeDictionary<Recipient, HashSet<IMapping>> _recipientsMap = new();

	/// <summary>
	/// 类型组合对应的 <see cref="Mapping"/> 和 <see cref="Mapping{TToken}"/> 实例。
	/// </summary>
	/// <remarks>
	/// 值的类型只是 <see cref="ITypeDictionary{T}"/>，因为我们无法预先知道类型参数。
	/// 每个方法依赖 <see cref="GetOrAddMapping{TMessage,TToken}"/> 来获取所用每一对泛型参数
	/// 对应的 <see cref="Mapping"/> 或 <see cref="Mapping{TToken}"/> 类的类型安全实例。
	/// </remarks>
	private readonly TypeDictionary<EquatableType, IMapping> _typesMap = new();

	/// <summary>
	/// 获取默认的 <see cref="StrongReferenceMessenger"/> 实例。
	/// </summary>
	public static StrongReferenceMessenger Default { get; } = new();

	/// <inheritdoc/>
	public bool IsRegistered<TMessage, TToken>(object recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		lock (_recipientsMap)
		{
			if (typeof(TToken) == typeof(Unit))
			{
				if (!TryGetMapping<TMessage>(out var mapping))
				{
					return false;
				}

				Recipient key = new(recipient);

				return mapping.ContainsKey(key);
			}
			else
			{
				if (!TryGetMapping<TMessage, TToken>(out var mapping))
				{
					return false;
				}

				Recipient key = new(recipient);

				return
					mapping.TryGetValue(key, out var handlers) &&
					handlers.ContainsKey(token);
			}
		}
	}

	/// <inheritdoc/>
	public void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
		where TRecipient : class
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);
		ArgumentAssert.ThrowIfNull(handler);

		Register<TMessage, TToken>(recipient, token, new MessageHandlerDispatcher.For<TRecipient, TMessage>(handler));
	}

	/// <inheritdoc cref="WeakReferenceMessenger.Register{TMessage, TToken}(IRecipient{TMessage}, TToken)"/>
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
	/// <typeparam name="TToken">用于选择要接收的消息的令牌类型。</typeparam>
	/// <param name="recipient">将接收消息的接收方。</param>
	/// <param name="token">用于确定接收通道的令牌。</param>
	/// <param name="dispatcher">要注册的 <see cref="MessageHandlerDispatcher"/> 实例，或 null。</param>
	/// <exception cref="InvalidOperationException">尝试重复注册同一条消息时抛出。</exception>
	private void Register<TMessage, TToken>(object recipient, TToken token, MessageHandlerDispatcher? dispatcher)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		lock (_recipientsMap)
		{
			Recipient key = new(recipient);
			IMapping mapping;

			// 针对 unit 令牌的快速路径
			if (typeof(TToken) == typeof(Unit))
			{
				// 获取此接收方的 <TMessage> 注册列表
				var underlyingMapping = GetOrAddMapping<TMessage>();
				ref var registeredHandler = ref underlyingMapping.GetOrAddValueRef(key);

				if (registeredHandler is not null)
				{
					ThrowInvalidOperationExceptionForDuplicateRegistration();
				}

				// 存储输入的处理程序
				registeredHandler = dispatcher;

				mapping = underlyingMapping;
			}
			else
			{
				// 获取此接收方的 <TMessage, TToken> 注册列表
				var underlyingMapping = GetOrAddMapping<TMessage, TToken>();
				ref var map = ref underlyingMapping.GetOrAddValueRef(key);

				map ??= new TypeDictionary<TToken, object>();

				// 添加新的注册项
				ref var registeredHandler = ref map.GetOrAddValueRef(token);

				if (registeredHandler is not null)
				{
					ThrowInvalidOperationExceptionForDuplicateRegistration();
				}

				registeredHandler = dispatcher;
				mapping = underlyingMapping;
			}

			// 确保此注册映射已被当前接收方跟踪
			ref var set = ref _recipientsMap.GetOrAddValueRef(key);

			set ??= new HashSet<IMapping>();

			_ = set.Add(mapping);
		}
	}

	/// <inheritdoc/>
	public void UnregisterAll(object recipient)
	{
		ArgumentAssert.ThrowIfNull(recipient);

		lock (_recipientsMap)
		{
			// 如果接收方完全没有注册任何消息，则忽略
			Recipient key = new(recipient);

			if (!_recipientsMap.TryGetValue(key, out var set))
			{
				return;
			}

			// 移除该接收方的所有已注册处理程序列表
			foreach (var mapping in set)
			{
				if (mapping.TryRemove(key) &&
					mapping.Count == 0)
				{
					// 此处的映射实际上是 Mapping<,> 类型，且类型参数未知。
					// 如果移除当前接收方后某个映射变为空，说明对于给定的消息类型
					// 和令牌类型组合已不存在任何已注册接收方。在这种情况下，
					// 我们也会从类型映射中移除该映射。在每个映射中保留键的原因是：
					// 从字典（哈希集合）中移除项在最好情况下只需 O(1) 开销，而
					// 若每次尝试遍历整个字典，则每次单独的移除操作至少需要
					// 付出 O(n) 的代价。
					_ = _typesMap.TryRemove(mapping.TypeArguments);
				}
			}

			// 移除接收方映射中关联的集合
			_ = _recipientsMap.TryRemove(key);
		}
	}

	/// <inheritdoc/>
	public void UnregisterAll<TToken>(object recipient, TToken token)
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		// 此方法从不会以 unit 类型调用，因此未实现该路径。此异常不应
		// 被抛出，它只是为了双重检查是否存在回归——以防引入的 bug
		// 导致该路径以某种方式以 Unit 类型被调用。此类型是 internal 的，
		// 因此库的使用者永远无法将其传入此处，并且库中也没有（也不应有）
		// 任何公开暴露的 API 会导致走到此路径。使用默认通道时，仅支持
		// UnregisterAll(object)，它只会注销所有接收方，而不考虑所选通道。
		if (typeof(TToken) == typeof(Unit))
		{
			throw new NotImplementedException();
		}

		var lockTaken = false;
		object[]? maps = null;
		var i = 0;

		// 此处我们显式使用 try/finally 块而不是 lock 语法，这样就能用同一个块
		// 既释放锁，又清空租用的缓冲区并将其归还到池中。之所以在此处声明缓冲区、
		// 并在此外层 finally 块中清空并归还它，是因为这样做无需保持锁，且在执行
		// 这最后一步之前释放锁可以减少持有锁的总时间，从而减少在多线程场景中
		// 并发调用此方法时的锁争用。
		try
		{
			Monitor.Enter(_recipientsMap, ref lockTaken);

			// 获取该接收方共享的映射集合（如果存在）
			Recipient key = new(recipient);

			if (!_recipientsMap.TryGetValue(key, out var set))
			{
				return;
			}

			// 将目标接收方的候选映射复制到本地数组，因为遍历集合时无法修改其内容。
			// 租用的缓冲区会偏大，还会包含通过其他令牌注册的消息处理程序的映射。
			// 注意，我们仅使用 object 数组以尽量减少租用缓冲区的总数——否则这些缓冲区
			// 除了在此处被租用外，只会在共享池中闲置。相反，我们使用一种库的使用者
			// 也可能用到的类型，这增加了双方复用现有缓冲区的机会。当需要以已知类型
			// 引用缓冲区中存储的项时，我们使用 Unsafe.As<T> 以避免强制转换中昂贵的
			// 类型检查，因为我们已经知道赋值是有效的。
			maps = ArrayPool<object>.Shared.Rent(set.Count);

			foreach (var item in set)
			{
				// 选择所有使用相同令牌类型的映射
				if (item is ITypeDictionary<Recipient, ITypeDictionary<TToken>> mapping)
				{
					maps[i++] = mapping;
				}
			}

			// 遍历所有本地映射。这些都是目标接收方当前存在的、令牌类型为当前类型、
			// 消息类型为任意给定类型的处理程序映射。我们在此严重依赖接口，
			// 以便能够遍历所有可用的映射，而无需预先知道具体类型，也无需
			// 处理反射：我们只需检查封闭接口的类型是否与当前使用的令牌类型匹配，
			// 然后对这些实例进行操作。
			foreach (var obj in maps.AsSpan(0, i))
			{
				var handlersMap = Unsafe.As<ITypeDictionary<Recipient, ITypeDictionary<TToken>>>(obj);

				// 我们不需要检查映射是否包含该接收方，因为映射序列已经
				// 从包含目标接收方所有映射的集合中复制而来：它必定存在。
				var holder = handlersMap[key];

				// 尝试移除输入令牌、当前消息类型（此处未知）对应的已注册处理程序。
				if (holder.TryRemove(token) &&
					holder.Count == 0)
				{
					// 如果映射为空，则从容器中完全移除该接收方
					_ = handlersMap.TryRemove(key);

					var mapping = Unsafe.As<IMapping>(handlersMap);

					// 该接收方已没有此令牌和消息类型组合的注册项，
					// 因此可以从其关联集合中移除该映射。
					_ = set.Remove(mapping);

					// 如果结果集合为空，说明该接收方已没有针对任何消息或令牌类型
					// 的处理程序，因此也可以从所有至少有一个处理程序的现存接收方映射中
					// 移除该接收方。
					if (set.Count == 0)
					{
						_ = _recipientsMap.TryRemove(key);
					}

					// 如果跨所有消息类型和令牌类型，任何接收方都已无处理程序剩余，
					// 则完全移除当前接收方的映射集合，并同时移除对其的强引用。
					// 这与直接调用 UnregisterAll(recipient) 所达到的效果相同。
					if (handlersMap.Count == 0)
					{
						_ = _typesMap.TryRemove(mapping.TypeArguments);
					}
				}
			}
		}
		finally
		{
			// 如果已获取锁，则释放它
			if (lockTaken)
			{
				Monitor.Exit(_recipientsMap);
			}

			// 如果已租用映射数组，则将其归还到共享池。
			// 清除引用以避免共享内存池产生泄漏。
			// 我们手动创建 span 并清空它作为一种小优化，因为
			// 从池中租用的数组可能大于请求的大小。
			if (maps is not null)
			{
				maps.AsSpan(0, i).Clear();

				ArrayPool<object>.Shared.Return(maps);
			}
		}
	}

	/// <inheritdoc/>
	public void Unregister<TMessage, TToken>(object recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		lock (_recipientsMap)
		{
			if (typeof(TToken) == typeof(Unit))
			{
				// 获取注册列表（如果可用）
				if (!TryGetMapping<TMessage>(out var mapping))
				{
					return;
				}

				Recipient key = new(recipient);

				// 移除处理程序（unit 类型只能有一个）
				if (!mapping.TryRemove(key))
				{
					return;
				}

				// 从此容器中完全移除该映射，并移除映射本身与现存已注册接收方
				// （或整个接收方）之间当前映射的链接。这与下面的逻辑相同，
				// 只是对于 unit 类型只能有一个处理程序，因此移除它就意味着
				// 目标接收方已没有剩余的处理程序。
				_ = mapping.TryRemove(key);

				// 如果此类型组合已无任何处理程序剩余，则将其丢弃
				if (mapping.Count == 0)
				{
					_ = _typesMap.TryRemove(mapping.TypeArguments);
				}

				var set = _recipientsMap[key];

				// 当前映射已不再为该接收方保留任何处理程序。
				// 移除它，如果这是最后一个处理程序，则同时移除该接收方。
				// 同样，这与下面的逻辑相同，只是以 unit 类型为前提。
				_ = set.Remove(mapping);

				if (set.Count == 0)
				{
					_ = _recipientsMap.TryRemove(key);
				}
			}
			else
			{
				// 获取注册列表（如果可用）
				if (!TryGetMapping<TMessage, TToken>(out var mapping))
				{
					return;
				}

				Recipient key = new(recipient);

				if (!mapping.TryGetValue(key, out var dictionary))
				{
					return;
				}

				// 移除目标处理程序
				if (dictionary.TryRemove(token) &&
					dictionary.Count == 0)
				{
					// 如果映射为空，说明当前接收方已没有针对当前 <TMessage, TToken> 组合的
					// 已注册处理程序，无论具体令牌值是什么（即用于接收该类型消息的通道）。
					// 我们可以从此容器中完全移除该映射，并移除映射本身与现存已注册接收方
					// （或整个接收方）之间当前映射的链接。
					_ = mapping.TryRemove(key);

					// 如果此类型组合已无任何处理程序剩余，则将其丢弃
					if (mapping.Count == 0)
					{
						_ = _typesMap.TryRemove(mapping.TypeArguments);
					}

					var set = _recipientsMap[key];

					// 当前映射已不再为该接收方保留任何处理程序
					_ = set.Remove(mapping);

					// 如果当前接收方已无任何处理程序剩余，则移除它
					if (set.Count == 0)
					{
						_ = _recipientsMap.TryRemove(key);
					}
				}
			}
		}
	}

	/// <inheritdoc/>
	public TMessage Send<TMessage, TToken>(TMessage message, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(message);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		object[] rentedArray;
		Span<object> pairs;
		var i = 0;

		lock (_recipientsMap)
		{
			if (typeof(TToken) == typeof(Unit))
			{
				// 检查是否存在任何已注册的接收方
				if (!TryGetMapping<TMessage>(out var mapping))
				{
					goto End;
				}

				// 检查剩余处理程序的数量，详见下文
				var totalHandlersCount = mapping.Count;

				if (totalHandlersCount == 0)
				{
					goto End;
				}

				pairs = rentedArray = ArrayPool<object>.Shared.Rent(2 * totalHandlersCount);

				// 逻辑与下文相同，只是此处每个接收方仅遍历一个处理程序
				var mappingEnumerator = mapping.GetEnumerator();

				while (mappingEnumerator.MoveNext())
				{
					pairs[2 * i] = mappingEnumerator.GetValue();
					pairs[(2 * i) + 1] = mappingEnumerator.GetKey().Target;
					i++;
				}
			}
			else
			{
				// 检查是否存在任何已注册的接收方
				if (!TryGetMapping<TMessage, TToken>(out var mapping))
				{
					goto End;
				}

				// 我们需要对当前已注册的处理程序做本地副本，因为用户可能会在
				// 某个现有处理程序内部尝试注销（或注册）新的处理程序。我们可以
				// 使用内存池来复用数组，以尽量减少平均内存占用。实践中，我们
				// 通常只需承担复制项的小额开销。当前映射包含所用 <TMessage, TToken>
				// 组合的所有当前已注册接收方和处理程序。在最坏情况下，所有接收方
				// 都有一个令牌与输入令牌匹配的已注册处理程序，这意味着我们最多
				// 可能需要调用等于映射中接收方总数的待处理处理程序。这依赖于
				// 令牌唯一且给定令牌只关联一个处理程序这一事实。我们可以将这个
				// 上界作为从池中租用每个数组的请求大小，从而保证有足够的空间。
				var totalHandlersCount = mapping.Count;

				if (totalHandlersCount == 0)
				{
					goto End;
				}

				// 租用数组并同时将其赋给 span，用于访问值。
				// 这样做是为了避免下文循环中数组协变的检查拖慢性能。
				pairs = rentedArray = ArrayPool<object>.Shared.Rent(2 * totalHandlersCount);

				// 将处理程序复制到本地集合。
				// 此时数组是偏大的，因为它还包含用于不同令牌的处理程序。
				// 我们可以复用同一个变量来统计稍后要调用的匹配处理程序数量。
				// 这将是租用缓冲区中有效处理程序所在的数组切片。
				var mappingEnumerator = mapping.GetEnumerator();

				// 此处显式使用枚举器，因为我们使用的是自定义枚举器，
				// 它不暴露标准的单个 Current 属性。
				while (mappingEnumerator.MoveNext())
				{
					// 如果令牌与接收方匹配，则选取目标处理程序
					if (mappingEnumerator.GetValue().TryGetValue(token, out var handler))
					{
						// 由于数组大小是根据当前已注册处理程序总数设置的，
						// 而该总数总是大于或等于匹配上述检查的数量，因此此 span
						// 访问应始终保证有效。不过我们仍在此使用带检查的 span 访问，
						// 以确保即使上述逻辑存在错误也绝不会发生越界写入。
						pairs[2 * i] = handler;
						pairs[(2 * i) + 1] = mappingEnumerator.GetKey().Target;
						i++;
					}
				}
			}
		}

		try
		{
			// 核心广播逻辑与弱引用消息信使相同
			WeakReferenceMessenger.SendAll(pairs, i, message);
		}
		finally
		{
			// 和之前一样，我们还需要先清空它，以避免残留引用存储在池中
			// 而导致潜在的长期内存泄漏。
			Array.Clear(rentedArray, 0, 2 * i);

			ArrayPool<object>.Shared.Return(rentedArray);
		}

	End:
		return message;
	}

	/// <summary>
	/// 将指定类型的消息发送给所有已注册的接收方。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识发送消息时使用哪个通道的令牌类型。</typeparam>
	/// <param name="message">要发送的消息。</param>
	/// <param name="token">指示使用哪个通道的令牌。</param>
	/// <returns>已发送的消息（即 <paramref name="message"/>）。</returns>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="message"/> 或 <paramref name="token"/> 为 <see langword="null"/> 时抛出。</exception>
	internal TMessage UnsafeSend<TMessage, TToken>(TMessage message, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(message);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		object[] rentedArray;
		Span<object> pairs;
		var i = 0;

		lock (_recipientsMap)
		{
			if (typeof(TToken) == typeof(Unit))
			{
				// 检查是否存在任何已注册的接收方
				if (!TryGetMapping<TMessage>(out var mapping))
				{
					throw new MessageDeliverException(string.Format(Resources.IDS_NO_RECIPIENTS_FOR_MESSAGE_TYPE, DescribeMessage(message)));
				}

				// 检查剩余处理程序的数量，详见下文
				var totalHandlersCount = mapping.Count;

				if (totalHandlersCount == 0)
				{
					throw new MessageDeliverException(string.Format(Resources.IDS_NO_RECIPIENTS_FOR_MESSAGE_TYPE, DescribeMessage(message)));
				}

				pairs = rentedArray = ArrayPool<object>.Shared.Rent(2 * totalHandlersCount);

				// 逻辑与下文相同，只是此处每个接收方仅遍历一个处理程序
				var mappingEnumerator = mapping.GetEnumerator();

				while (mappingEnumerator.MoveNext())
				{
					pairs[2 * i] = mappingEnumerator.GetValue();
					pairs[(2 * i) + 1] = mappingEnumerator.GetKey().Target;
					i++;
				}
			}
			else
			{
				// 检查是否存在任何已注册的接收方
				if (!TryGetMapping<TMessage, TToken>(out var mapping))
				{
					throw new InvalidOperationException(string.Format(Resources.IDS_NO_RECIPIENTS_FOR_MESSAGE_TYPE_AND_TOKEN, DescribeMessage(message), token));
				}

				// 我们需要对当前已注册的处理程序做本地副本，因为用户可能会在
				// 某个现有处理程序内部尝试注销（或注册）新的处理程序。我们可以
				// 使用内存池来复用数组，以尽量减少平均内存占用。实践中，我们
				// 通常只需承担复制项的小额开销。当前映射包含所用 <TMessage, TToken>
				// 组合的所有当前已注册接收方和处理程序。在最坏情况下，所有接收方
				// 都有一个令牌与输入令牌匹配的已注册处理程序，这意味着我们最多
				// 可能需要调用等于映射中接收方总数的待处理处理程序。这依赖于
				// 令牌唯一且给定令牌只关联一个处理程序这一事实。我们可以将这个
				// 上界作为从池中租用每个数组的请求大小，从而保证有足够的空间。
				var totalHandlersCount = mapping.Count;

				if (totalHandlersCount == 0)
				{
					throw new MessageDeliverException(string.Format(Resources.IDS_NO_RECIPIENTS_FOR_MESSAGE_TYPE, DescribeMessage(message)));
				}

				// 租用数组并同时将其赋给 span，用于访问值。
				// 这样做是为了避免下文循环中数组协变的检查拖慢性能。
				pairs = rentedArray = ArrayPool<object>.Shared.Rent(2 * totalHandlersCount);

				// 将处理程序复制到本地集合。
				// 此时数组是偏大的，因为它还包含用于不同令牌的处理程序。
				// 我们可以复用同一个变量来统计稍后要调用的匹配处理程序数量。
				// 这将是租用缓冲区中有效处理程序所在的数组切片。
				var mappingEnumerator = mapping.GetEnumerator();

				// 此处显式使用枚举器，因为我们使用的是自定义枚举器，
				// 它不暴露标准的单个 Current 属性。
				while (mappingEnumerator.MoveNext())
				{
					// 如果令牌与接收方匹配，则选取目标处理程序
					if (mappingEnumerator.GetValue().TryGetValue(token, out var handler))
					{
						// 由于数组大小是根据当前已注册处理程序总数设置的，
						// 而该总数总是大于或等于匹配上述检查的数量，因此此 span
						// 访问应始终保证有效。不过我们仍在此使用带检查的 span 访问，
						// 以确保即使上述逻辑存在错误也绝不会发生越界写入。
						pairs[2 * i] = handler;
						pairs[(2 * i) + 1] = mappingEnumerator.GetKey().Target;
						i++;
					}
				}
			}
		}

		if (i == 0)
		{
			Array.Clear(rentedArray, 0, 0);
			ArrayPool<object>.Shared.Return(rentedArray);

			throw new MessageDeliverException(string.Format(Resources.IDS_NO_RECIPIENTS_FOR_MESSAGE_TYPE_AND_TOKEN_TO_DELIVER, DescribeMessage(message), token));
		}

		try
		{
			// 核心广播逻辑与弱引用消息信使相同
			WeakReferenceMessenger.SendAll(pairs, i, message);
		}
		finally
		{
			// 和之前一样，我们还需要先清空它，以避免残留引用存储在池中
			// 而导致潜在的长期内存泄漏。
			Array.Clear(rentedArray, 0, 2 * i);

			ArrayPool<object>.Shared.Return(rentedArray);
		}

		return message;
	}

	/// <inheritdoc/>
	void IMessenger.Cleanup()
	{
		// 当前实现不需要任何形式的清理操作，因为每当添加或移除接收方时，
		// 所有内部数据结构都已保持同步。此方法通过显式接口实现来提供，
		// 以便直接使用此类型的开发者不会在 API 表面上看到它
		// （反正它也没有用处，因为此处它是空操作）。
	}

	/// <inheritdoc/>
	public void Reset()
	{
		lock (_recipientsMap)
		{
			_recipientsMap.Clear();
			_typesMap.Clear();
		}
	}

	/// <summary>
	/// 尝试获取输入 <typeparamref name="TMessage"/> 类型当前已注册接收方的
	/// <see cref="Mapping"/> 实例。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <param name="mapping">找到时返回的 <see cref="Mapping"/> 实例。</param>
	/// <returns>是否找到了所需的 <see cref="Mapping"/> 实例。</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryGetMapping<TMessage>([NotNullWhen(true)] out Mapping? mapping)
		where TMessage : class
	{
		EquatableType key = new(typeof(TMessage), typeof(Unit));

		if (_typesMap.TryGetValue(key, out var target))
		{
			// 此方法及以下方法是类型映射中唯一处理值的方法，
			// 此处我们确信所持有的对象引用指向正确类型的实例。
			// 使用非安全强制转换可跳过两个条件分支，速度更快。
			mapping = Unsafe.As<Mapping>(target);

			return true;
		}

		mapping = null;

		return false;
	}

	/// <summary>
	/// 尝试获取类型 <typeparamref name="TMessage"/> 和 <typeparamref name="TToken"/> 组合
	/// 当前已注册接收方的 <see cref="Mapping{TToken}"/> 实例。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识发送消息时使用哪个通道的令牌类型。</typeparam>
	/// <param name="mapping">找到时返回的 <see cref="Mapping{TToken}"/> 实例。</param>
	/// <returns>是否找到了所需的 <see cref="Mapping{TToken}"/> 实例。</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryGetMapping<TMessage, TToken>([NotNullWhen(true)] out Mapping<TToken>? mapping)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		EquatableType key = new(typeof(TMessage), typeof(TToken));

		if (_typesMap.TryGetValue(key, out var target))
		{
			mapping = Unsafe.As<Mapping<TToken>>(target);

			return true;
		}

		mapping = null;

		return false;
	}

	/// <summary>
	/// 获取输入 <typeparamref name="TMessage"/> 类型当前已注册接收方的
	/// <see cref="Mapping"/> 实例。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <returns>具有所请求类型参数的 <see cref="Mapping"/> 实例。</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Mapping GetOrAddMapping<TMessage>()
		where TMessage : class
	{
		EquatableType key = new(typeof(TMessage), typeof(Unit));
		ref var target = ref _typesMap.GetOrAddValueRef(key);

		target ??= Mapping.Create<TMessage>();

		return Unsafe.As<Mapping>(target);
	}

	/// <summary>
	/// 获取类型 <typeparamref name="TMessage"/> 和 <typeparamref name="TToken"/> 组合
	/// 当前已注册接收方的 <see cref="Mapping{TToken}"/> 实例。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <typeparam name="TToken">用于标识发送消息时使用哪个通道的令牌类型。</typeparam>
	/// <returns>具有所请求类型参数的 <see cref="Mapping{TToken}"/> 实例。</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Mapping<TToken> GetOrAddMapping<TMessage, TToken>()
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		EquatableType key = new(typeof(TMessage), typeof(TToken));
		ref var target = ref _typesMap.GetOrAddValueRef(key);

		target ??= Mapping<TToken>.Create<TMessage>();

		return Unsafe.As<Mapping<TToken>>(target);
	}

	/// <summary>
	/// 表示接收方链接及其按通信通道划分的处理程序视图的映射类型。
	/// </summary>
	/// <remarks>
	/// 此类型是 <see cref="Mapping{TToken}"/> 针对 <see cref="Unit"/> 令牌的特化。
	/// </remarks>
	private sealed class Mapping : TypeDictionary<Recipient, object>, IMapping
	{
		/// <summary>
		/// 初始化 <see cref="Mapping"/> 类的新实例。
		/// </summary>
		/// <param name="messageType">正在使用的消息类型。</param>
		private Mapping(Type messageType)
		{
			TypeArguments = new EquatableType(messageType, typeof(Unit));
		}

		/// <summary>
		/// 创建 <see cref="Mapping"/> 类的新实例。
		/// </summary>
		/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
		/// <returns>新的 <see cref="Mapping"/> 实例。</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Mapping Create<TMessage>()
			where TMessage : class
		{
			return new(typeof(TMessage));
		}

		/// <inheritdoc/>
		public EquatableType TypeArguments { get; }
	}

	/// <summary>
	/// 表示接收方链接及其按通信通道划分的处理程序视图的映射类型。
	/// </summary>
	/// <typeparam name="TToken">用于选择要接收的消息的令牌类型。</typeparam>
	/// <remarks>
	/// 定义此类型是为了简化实现，并作为 C# 不支持在开放泛型类型上使用类型别名
	/// 的一种变通方案（类型别名只能用于具体的封闭类型）。
	/// </remarks>
	private sealed class Mapping<TToken> : TypeDictionary<Recipient, TypeDictionary<TToken, object>>, IMapping
		where TToken : IEquatable<TToken>
	{
		/// <summary>
		/// 初始化 <see cref="Mapping{TToken}"/> 类的新实例。
		/// </summary>
		/// <param name="messageType">正在使用的消息类型。</param>
		private Mapping(Type messageType)
		{
			TypeArguments = new EquatableType(messageType, typeof(TToken));
		}

		/// <summary>
		/// 创建 <see cref="Mapping{TToken}"/> 类的新实例。
		/// </summary>
		/// <typeparam name="TMessage">要接收的消息类型。</typeparam>
		/// <returns>新的 <see cref="Mapping{TToken}"/> 实例。</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Mapping<TToken> Create<TMessage>()
			where TMessage : class
		{
			return new(typeof(TMessage));
		}

		/// <inheritdoc/>
		public EquatableType TypeArguments { get; }
	}

	/// <summary>
	/// <see cref="Mapping"/> 和 <see cref="Mapping{TToken}"/> 类型的接口，用于在事先不了解
	/// 类型参数的情况下，从给定的泛型实例中获取这些参数。
	/// </summary>
	private interface IMapping : ITypeDictionary<Recipient>
	{
		/// <summary>
		/// 获取表示当前类型参数的 <see cref="EquatableType"/> 实例。
		/// </summary>
		EquatableType TypeArguments { get; }
	}

	/// <summary>
	/// 表示接收方的简单类型。
	/// </summary>
	/// <remarks>
	/// 此类型用于在每个映射字典中实现快速索引，因为它为任意对象充当
	/// <see cref="GetHashCode"/> 和 <see cref="Equals(object?)"/> 方法的外部替代实现，
	/// 消除了虚调用，并防止实例在此上下文中重写这些方法。
	/// 使用此类型可保证对所有已注册接收方的相等性操作始终仅基于引用相等，
	/// 而与其类型无关。
	/// </remarks>
	private readonly struct Recipient : IEquatable<Recipient>
	{
		/// <summary>
		/// 已注册的接收方。
		/// </summary>
		public readonly object Target;

		/// <summary>
		/// 初始化 <see cref="Recipient"/> 结构的新实例。
		/// </summary>
		/// <param name="target">目标接收方实例。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Recipient(object target)
		{
			Target = target;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Recipient other)
		{
			return ReferenceEquals(Target, other.Target);
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj)
		{
			return obj is Recipient other && Equals(other);
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(Target);
		}
	}

	/// <summary>
	/// 尝试添加重复的处理程序时抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	private static void ThrowInvalidOperationExceptionForDuplicateRegistration()
	{
		throw new InvalidOperationException(Resources.IDS_RECIPIENT_ALREADY_SUBSCRIBED);
	}

	/// <summary>
	/// 构造用于异常消息的消息描述。
	/// </summary>
	/// <param name="message">正在投递的消息。</param>
	/// <typeparam name="TMessage">消息的 CLR 类型。</typeparam>
	/// <returns>包含业务消息类型、消息 ID 与通道的描述文本。</returns>
	/// <remarks>
	/// 内存总线统一用 <see cref="MessagePack"/> 包装消息，直接输出 <typeparamref name="TMessage"/>
	/// 只能看到包装类型，无法判断是哪条业务消息缺少订阅者。
	/// </remarks>
	private static string DescribeMessage<TMessage>(TMessage message)
		where TMessage : class
	{
		return message is MessagePack pack
			? $"'{pack.Message.TypeName}' (message id: '{pack.Message.MessageId}', channel: '{pack.Message.Channel}')"
			: $"'{typeof(TMessage).FullName}'";
	}
}