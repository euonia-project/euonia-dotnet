using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// A class providing a reference implementation for the <see cref="IMessenger"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This <see cref="IMessenger"/> implementation uses weak references to track the registered
/// recipients, so it is not necessary to manually unregister them when they're no longer needed.
/// </para>
/// <para>
/// The <see cref="WeakReferenceMessenger"/> type will automatically perform internal trimming when
/// full GC collections are invoked, so calling <see cref="Cleanup"/> manually is not necessary to
/// ensure that on average the internal data structures are as trimmed and compact as possible.
/// </para>
/// </remarks>
public sealed class WeakReferenceMessenger : IMessenger
{
	// This messenger uses the following logic to link stored instances together:
	// --------------------------------------------------------------------------------------------------------
	//                          ITypeDictionary<TToken, MessageHandlerDispatcher?> mapping
	//                                        /                   /             /
	//                   ___(EquatableType.TToken)___/                   /             /         ___(if EquatableType.TToken is Unit)
	//                  /_________(EquatableType.TMessage)______________/             /         /
	//                 /                                    _________________/___MessageHandlerDispatcher?
	//                /                                    /                         \
	// ITypeDictionary<EquatableType, ConditionalWeakTable<object, object?>> recipientsMap;       \___(null if using IRecipient<TMessage>)
	// --------------------------------------------------------------------------------------------------------
	// Just like in the strong reference variant, each pair of message and token types is used as a key in the
	// recipients map. In this case, the values in the dictionary are ConditionalWeakTable2<,> instances, that
	// link each registered recipient to a map of currently registered handlers, through dependent handles. This
	// ensures that handlers will remain alive as long as their associated recipient is also alive (so there is no
	// need for users to manually indicate whether a given handler should be kept alive in case it creates a closure).
	// The value in each conditional table can either be ITypeDictionary<TToken, MessageHandlerDispatcher> or object. The
	// first case is used when any token type other than the default Unit type is used, as in this case there could be
	// multiple handlers for each recipient that need to be tracked separately. In order to invoke all the handlers from
	// a context where their type parameters is not known, handlers are stored as MessageHandlerDispatcher instances. There
	// are two possible cases here: either a given instance is of type MessageHandlerDispatcher.For<TRecipient, TMessage>,
	// or null. The first is the default case: whenever a subscription is done with a MessageHandler<TRecipient, TToken>,
	// that delegate is wrapped in an instance of this class so that it can keep track internally of the generic context in
	// use, so that it can be retrieved when the callback is executed. If the subscription is done directly on a recipient
	// that implements IRecipient<TMessage instead, the dispatcher is null, which just acts as marker. Whenever the broadcast
	// method finds it, it will just invoke IRecipient<TMessage.Receive directly on the target recipient, which avoids the
	// extra indirection on dispatch as well as having to allocate an extra wrapper type for the handler. Lastly, there is a
	// special case when subscriptions are done through the Unit type, meaning when the default channel is in use. In this
	// case, each recipient only stores a single MessageHandlerDispatcher instance and not a whole dictionary, as there can
	// only ever be a single handler for each recipient.

	/// <summary>
	/// The map of currently registered recipients for all message types.
	/// </summary>
	private readonly TypeDictionary<EquatableType, ConditionalWeakTable2<object, object>> _recipientsMap = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="WeakReferenceMessenger"/> class.
	/// </summary>
	public WeakReferenceMessenger()
	{
		// Proxy function for the GC callback. This needs to be static and to take the target instance as
		// an input parameter in order to avoid rooting it from the Gen2GcCallback object invoking it.
		static void Gen2GcCallbackProxy(object target)
		{
			((WeakReferenceMessenger)target).CleanupWithNonBlockingLock();
		}

		// Register an automatic GC callback to trigger a non-blocking cleanup. This will ensure that the
		// current messenger instance is trimmed and without leftover recipient maps that are no longer used.
		// This is necessary (as in, some form of cleanup, either explicit or automatic like in this case)
		// because the ConditionalWeakTable<TKey, TValue> instances will just remove key-value pairs on their
		// own as soon as a key (ie. a recipient) is collected, causing their own keys (ie. the EquatableType instances
		// mapping to each conditional table for a pair of message and token types) to potentially remain in the
		// root mapping structure but without any remaining recipients actually registered there, which just
		// adds unnecessary overhead when trying to enumerate recipients during broadcasting operations later on.
		Gen2GcCallback.Register(Gen2GcCallbackProxy, this);
	}

	/// <summary>
	/// Gets the default <see cref="WeakReferenceMessenger"/> instance.
	/// </summary>
	public static WeakReferenceMessenger Default { get; } = new();

	/// <inheritdoc/>
	public bool IsRegistered<TMessage, TToken>(object recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		lock (_recipientsMap)
		{
			EquatableType equatableType = new(typeof(TMessage), typeof(TToken));

			// Get the conditional table associated with the target recipient, for the current pair
			// of token and message types. If it exists, check if there is a matching token.
			if (!_recipientsMap.TryGetValue(equatableType, out var table))
			{
				return false;
			}

			// Special case for unit tokens
			if (typeof(TToken) == typeof(Unit))
			{
				return table.TryGetValue(recipient, out _);
			}

			// Custom token type, so each recipient has an associated map
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
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);
		ArgumentAssert.ThrowIfNull(handler);

		Register<TMessage, TToken>(recipient, token, new MessageHandlerDispatcher.For<TRecipient, TMessage>(handler));
	}

	/// <summary>
	/// Registers a recipient for a given type of message.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to receive.</typeparam>
	/// <typeparam name="TToken">The type of token to use to pick the messages to receive.</typeparam>
	/// <param name="recipient">The recipient that will receive the messages.</param>
	/// <param name="token">A token used to determine the receiving channel to use.</param>
	/// <exception cref="InvalidOperationException">Thrown when trying to register the same message twice.</exception>
	/// <remarks>
	/// This method is a variation of <see cref="Register{TRecipient, TMessage, TToken}(TRecipient, TToken, MessageHandler{TRecipient, TMessage})"/>
	/// that is specialized for recipients implementing <see cref="IRecipient{TMessage}"/>. See more comments at the top of this type, as well as
	/// within <see cref="Send{TMessage, TToken}(TMessage, TToken)"/> and in the <see cref="MessageHandlerDispatcher"/> types.
	/// </remarks>
	internal void Register<TMessage, TToken>(IRecipient<TMessage> recipient, TToken token)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		Register<TMessage, TToken>(recipient, token, null);
	}

	/// <summary>
	/// Registers a recipient for a given type of message.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to receive.</typeparam>
	/// <typeparam name="TToken">The type of token to use to pick the messages to receive.</typeparam>
	/// <param name="recipient">The recipient that will receive the messages.</param>
	/// <param name="token">A token used to determine the receiving channel to use.</param>
	/// <param name="dispatcher">The input <see cref="MessageHandlerDispatcher"/> instance to register, or null.</param>
	/// <exception cref="InvalidOperationException">Thrown when trying to register the same message twice.</exception>
	private void Register<TMessage, TToken>(object recipient, TToken token, MessageHandlerDispatcher dispatcher)
		where TMessage : class
		where TToken : IEquatable<TToken>
	{
		lock (_recipientsMap)
		{
			EquatableType equatableType = new(typeof(TMessage), typeof(TToken));

			// Get the conditional table for the pair of type arguments, or create it if it doesn't exist
			ref var mapping = ref _recipientsMap.GetOrAddValueRef(equatableType);

			mapping ??= new ConditionalWeakTable2<object, object>();

			// Fast path for unit tokens
			if (typeof(TToken) == typeof(Unit))
			{
				if (!mapping.TryAdd(recipient, dispatcher))
				{
					ThrowInvalidOperationExceptionForDuplicateRegistration();
				}
			}
			else
			{
				// Get or create the handlers dictionary for the target recipient
				var map = Unsafe.As<TypeDictionary<TToken, object>>(mapping.GetValue(recipient, static _ => new TypeDictionary<TToken, object>())!);

				// Add the new registration entry
				ref var registeredHandler = ref map.GetOrAddValueRef(token);

				if (registeredHandler is not null)
				{
					ThrowInvalidOperationExceptionForDuplicateRegistration();
				}

				// Store the input handler
				registeredHandler = dispatcher;
			}
		}
	}

	/// <inheritdoc/>
	public void UnregisterAll(object recipient)
	{
		ArgumentAssert.ThrowIfNull(recipient);

		lock (_recipientsMap)
		{
			var enumerator = _recipientsMap.GetEnumerator();

			// Traverse all the existing conditional tables and remove all the ones
			// with the target recipient as key. We don't perform a cleanup here,
			// as that is responsibility of a separate method defined below.
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
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		// This method is never called with the unit type. See more details in
		// the comments in the corresponding method in StrongReferenceMessenger.
		if (typeof(TToken) == typeof(Unit))
		{
			throw new NotImplementedException();
		}

		lock (_recipientsMap)
		{
			var enumerator = _recipientsMap.GetEnumerator();

			// Same as above, with the difference being that this time we only go through
			// the conditional tables having a matching token type as key, and that we
			// only try to remove handlers with a matching token, if any.
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
		ArgumentAssert.ThrowIfNull(recipient);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		lock (_recipientsMap)
		{
			EquatableType equatableType = new(typeof(TMessage), typeof(TToken));

			// Get the target mapping table for the combination of message and token types,
			// and remove the handler with a matching token (the entire map), if present.
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
		ArgumentAssert.ThrowIfNull(message);
		ArgumentAssert.For<TToken>.ThrowIfNull(token);

		ArrayPoolBufferWriter<object> bufferWriter;
		var i = 0;

		lock (_recipientsMap)
		{
			EquatableType equatableType = new(typeof(TMessage), typeof(TToken));

			// Try to get the target table
			if (!_recipientsMap.TryGetValue(equatableType, out var table))
			{
				return message;
			}

			bufferWriter = ArrayPoolBufferWriter<object>.Create();

			// We need a local, temporary copy of all the pending recipients and handlers to
			// invoke, to avoid issues with handlers unregistering from messages while we're
			// holding the lock. To do this, we can just traverse the conditional table in use
			// to enumerate all the existing recipients for the token and message types pair
			// corresponding to the generic arguments for this invocation, and then track the
			// handlers with a matching token, and their corresponding recipients.
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
	/// Implements the broadcasting logic for <see cref="Send{TMessage, TToken}(TMessage, TToken)"/>.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <param name="pairs"></param>
	/// <param name="i"></param>
	/// <param name="message"></param>
	/// <remarks>
	/// This method is not a local function to avoid triggering multiple compilations due to <c>TToken</c>
	/// potentially being a value type, which results in specialized code due to reified generics. This is
	/// necessary to work around a Roslyn limitation that causes unnecessary type parameters in local
	/// functions not to be discarded in the synthesized methods. Additionally, keeping this loop outside
	/// of the EH block (the <see langword="try"/> block) can help result in slightly better codegen.
	/// </remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void SendAll<TMessage>(ReadOnlySpan<object> pairs, int i, TMessage message)
		where TMessage : class
	{
		// This Slice calls executes bounds checks for the loop below, in case i was somehow wrong.
		// The rest of the implementation relies on bounds checks removal and loop strength reduction
		// done manually (which results in a 20% speedup during broadcast), since the JIT is not able
		// to recognize this pattern. Skipping checks below is a provably safe optimization: the slice
		// has exactly 2 * i elements (due to this slicing), and each loop iteration processes a pair.
		// The loops ends when the initial reference reaches the end, and that's incremented by 2 at
		// the end of each iteration. The target being a span, obviously means the length is constant.
		var slice = pairs[..(2 * i)];

		ref var sliceStart = ref MemoryMarshal.GetReference(slice);
		ref var sliceEnd = ref Unsafe.Add(ref sliceStart, slice.Length);

		while (Unsafe.IsAddressLessThan(ref sliceStart, ref sliceEnd))
		{
			var handler = sliceStart;
			var recipient = Unsafe.Add(ref sliceStart, 1)!;

			// Here we need to distinguish the two possible cases: either the recipient was registered
			// through the IRecipient<TMessage> interface, or with a custom handler. In the first case,
			// the handler stored in the messenger is just null, so we can check that and branch to a
			// fast path that just invokes IRecipient<TMessage> directly on the recipient. Otherwise,
			// we will use the standard double dispatch approach. This check is particularly convenient
			// as we only need to check for null to determine what registration type was used, without
			// having to store any additional info in the messenger. This will produce code as follows,
			// with the advantage of also being compact and not having to use any additional registers:
			// =============================
			// L0000: test rcx, rcx
			// L0003: jne short L0040
			// =============================
			// Which is extremely fast. The reason for this conditional check in the first place is that
			// we're doing manual (null based) guarded devirtualization: if the handler is the marker
			// type and not an actual handler then we know that the recipient implements
			// IRecipient<TMessage>, so we can just cast to it and invoke it directly. This avoids
			// having to store the proxy callback when registering, and also skips an indirection
			// (invoking the delegate that then invokes the actual method). Additional note: this
			// pattern ensures that both casts below do not actually alias incompatible reference
			// types (as in, they would both succeed if they were safe casts), which lets the code
			// not rely on undefined behavior to run correctly (ie. we're not aliasing delegates).
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
	/// Executes a cleanup without locking the current instance. This method has to be
	/// invoked when a lock on <see cref="_recipientsMap"/> has already been acquired.
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
	/// Executes a cleanup without locking the current instance. This method has to be
	/// invoked when a lock on <see cref="_recipientsMap"/> has already been acquired.
	/// </summary>
	private void CleanupWithoutLock()
	{
		using var type = ArrayPoolBufferWriter<EquatableType>.Create();
		using var emptyRecipients = ArrayPoolBufferWriter<object>.Create();

		var type2Enumerator = _recipientsMap.GetEnumerator();

		// First, we go through all the currently registered pairs of token and message types.
		// These represents all the combinations of generic arguments with at least one registered
		// handler, with the exception of those with recipients that have already been collected.
		while (type2Enumerator.MoveNext())
		{
			emptyRecipients.Reset();

			var hasAtLeastOneHandler = false;

			if (type2Enumerator.GetKey().Token == typeof(Unit))
			{
				// When the token type is unit, there can be no registered recipients with no handlers,
				// as when the single handler is unsubscribed the recipient is also removed immediately.
				// Therefore, we need to check that there exists at least one recipient for the message.
				using var recipientsEnumerator = type2Enumerator.GetValue().GetEnumerator();

				while (recipientsEnumerator.MoveNext())
				{
					hasAtLeastOneHandler = true;

					break;
				}
			}
			else
			{
				// Go through the currently alive recipients to look for those with no handlers left. We track
				// the ones we find to remove them outside of the loop (can't modify during enumeration).
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

				// Remove the handler maps for recipients that are still alive but with no handlers
				foreach (var recipient in emptyRecipients.Span)
				{
					_ = type2Enumerator.GetValue().Remove(recipient);
				}
			}

			// Track the type combinations with no recipients or handlers left
			if (!hasAtLeastOneHandler)
			{
				type.Add(type2Enumerator.GetKey());
			}
		}

		// Remove all the mappings with no handlers left
		foreach (var key in type.Span)
		{
			_ = _recipientsMap.TryRemove(key);
		}
	}

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> when trying to add a duplicate handler.
	/// </summary>
	private static void ThrowInvalidOperationExceptionForDuplicateRegistration()
	{
		throw new InvalidOperationException(Resources.IDS_RECIPIENT_ALREADY_SUBSCRIBED);
	}
}