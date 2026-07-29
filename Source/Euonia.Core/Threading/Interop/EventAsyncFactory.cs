namespace Nerosoft.Euonia.Threading.Interop;

/// <summary>
/// 用于包装事件的 <see cref="Task"/> 的创建方法。
/// </summary>
public static class EventAsyncFactory
{
	/// <summary>
	/// 返回一个 <see cref="Task"/>，当指定的事件下一次触发时完成。此重载适用于任意类型的事件。
	/// </summary>
	/// <typeparam name="TDelegate">事件委托的类型。</typeparam>
	/// <typeparam name="TEventArguments">包含所有事件参数的类型。</typeparam>
	/// <param name="convert">一个转换委托，接收 <see cref="Action"/> 并将其转换为 <typeparamref name="TDelegate"/>。通常形式为 <c>x => (...) => x(new TEventArguments(...))</c>。</param>
	/// <param name="subscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其从事件中取消订阅。当 <paramref name="unsubscribeOnCapturedContext"/> 为 <c>true</c> 时，此方法在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <param name="unsubscribeOnCapturedContext">是否在捕获的上下文中调用 <paramref name="unsubscribe"/>。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static async Task<TEventArguments> FromAnyEvent<TDelegate, TEventArguments>(
		Func<Action<TEventArguments>, TDelegate> convert,
		Action<TDelegate> subscribe, Action<TDelegate> unsubscribe, CancellationToken cancellationToken,
		bool unsubscribeOnCapturedContext)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var tcs = Extensions.CreateAsyncTaskSource<TEventArguments>();
		var subscription = convert(result => tcs.TrySetResult(result));
		try
		{
			using (cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false))
			{
				subscribe(subscription);
				return await tcs.Task.ConfigureAwait(continueOnCapturedContext: unsubscribeOnCapturedContext);
			}
		}
		finally
		{
			unsubscribe(subscription);
		}
	}

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于任意类型的事件。
	/// </summary>
	/// <typeparam name="TDelegate">事件委托的类型。</typeparam>
	/// <typeparam name="TEventArguments">包含所有事件参数的类型。</typeparam>
	/// <param name="convert">一个转换委托，接收 <see cref="Action{TEventArguments}"/> 并将其转换为 <typeparamref name="TDelegate"/>。通常形式为 <c>x => (...) => x(new TEventArguments(...))</c>。</param>
	/// <param name="subscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<TEventArguments> FromAnyEvent<TDelegate, TEventArguments>(
		Func<Action<TEventArguments>, TDelegate> convert,
		Action<TDelegate> subscribe, Action<TDelegate> unsubscribe, CancellationToken cancellationToken)
		=> FromAnyEvent(convert, subscribe, unsubscribe, cancellationToken, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于任意类型的事件。
	/// </summary>
	/// <typeparam name="TDelegate">事件委托的类型。</typeparam>
	/// <typeparam name="TEventArguments">包含所有事件参数的类型。</typeparam>
	/// <param name="convert">一个转换委托，接收 <see cref="Action{TEventArguments}"/> 并将其转换为 <typeparamref name="TDelegate"/>。通常形式为 <c>x => (...) => x(new TEventArguments(...))</c>。</param>
	/// <param name="subscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<TEventArguments> FromAnyEvent<TDelegate, TEventArguments>(
		Func<Action<TEventArguments>, TDelegate> convert,
		Action<TDelegate> subscribe, Action<TDelegate> unsubscribe)
		=> FromAnyEvent(convert, subscribe, unsubscribe, CancellationToken.None, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="EventHandler"/> 的事件。
	/// </summary>
	/// <param name="subscribe">一个方法，接收 <see cref="EventHandler"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="EventHandler"/> 并将其从事件中取消订阅。当 <paramref name="unsubscribeOnCapturedContext"/> 为 <c>true</c> 时，此方法在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <param name="unsubscribeOnCapturedContext">是否在捕获的上下文中调用 <paramref name="unsubscribe"/>。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, EventArgs>> FromEvent(Action<EventHandler> subscribe,
																	Action<EventHandler> unsubscribe, CancellationToken cancellationToken, bool unsubscribeOnCapturedContext)
		=> FromAnyEvent<EventHandler, EventArguments<object, EventArgs>>(
			x => (sender, args) => x(CreateEventArguments(sender, args)), subscribe, unsubscribe, cancellationToken,
			unsubscribeOnCapturedContext);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="EventHandler"/> 的事件。
	/// </summary>
	/// <param name="subscribe">一个方法，接收 <see cref="EventHandler"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="EventHandler"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, EventArgs>> FromEvent(Action<EventHandler> subscribe,
																	Action<EventHandler> unsubscribe, CancellationToken cancellationToken)
		=> FromEvent(subscribe, unsubscribe, cancellationToken, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="EventHandler"/> 的事件。
	/// </summary>
	/// <param name="subscribe">一个方法，接收 <see cref="EventHandler"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="EventHandler"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, EventArgs>> FromEvent(Action<EventHandler> subscribe,
																	Action<EventHandler> unsubscribe)
		=> FromEvent(subscribe, unsubscribe, CancellationToken.None, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="EventHandler{TEventArgs}"/> 的事件。
	/// </summary>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="EventHandler{TEventArgs}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="EventHandler{TEventArgs}"/> 并将其从事件中取消订阅。当 <paramref name="unsubscribeOnCapturedContext"/> 为 <c>true</c> 时，此方法在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <param name="unsubscribeOnCapturedContext">是否在捕获的上下文中调用 <paramref name="unsubscribe"/>。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, TEventArgs>> FromEvent<TEventArgs>(
		Action<EventHandler<TEventArgs>> subscribe, Action<EventHandler<TEventArgs>> unsubscribe,
		CancellationToken cancellationToken, bool unsubscribeOnCapturedContext)
		=> FromAnyEvent<EventHandler<TEventArgs>, EventArguments<object, TEventArgs>>(
			x => (sender, args) => x(CreateEventArguments(sender, args)), subscribe, unsubscribe, cancellationToken,
			unsubscribeOnCapturedContext);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="EventHandler{TEventArgs}"/> 的事件。
	/// </summary>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="EventHandler{TEventArgs}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="EventHandler{TEventArgs}"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, TEventArgs>> FromEvent<TEventArgs>(
		Action<EventHandler<TEventArgs>> subscribe, Action<EventHandler<TEventArgs>> unsubscribe,
		CancellationToken cancellationToken)
		=> FromEvent(subscribe, unsubscribe, cancellationToken, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="EventHandler{TEventArgs}"/> 的事件。
	/// </summary>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="EventHandler{TEventArgs}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="EventHandler{TEventArgs}"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, TEventArgs>> FromEvent<TEventArgs>(
		Action<EventHandler<TEventArgs>> subscribe, Action<EventHandler<TEventArgs>> unsubscribe)
		=> FromEvent(subscribe, unsubscribe, CancellationToken.None, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于遵循标准 <c>sender, eventArgs</c> 模式但使用自定义委托类型的事件。
	/// </summary>
	/// <typeparam name="TDelegate">事件委托的类型。</typeparam>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="convert">一个转换委托，接收 <see cref="EventHandler{TEventArgs}"/> 并将其转换为 <typeparamref name="TDelegate"/>。如果显式指定类型参数，则应使用 <c>x => x.Invoke</c>。如果类型参数是推断的，则应使用 <c>(EventHandler&lt;TEventArgs&gt; x) => new TDelegate(x)</c>，并对 <typeparamref name="TEventArgs"/> 和 <typeparamref name="TDelegate"/> 进行相应的替换。</param>
	/// <param name="subscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其从事件中取消订阅。当 <paramref name="unsubscribeOnCapturedContext"/> 为 <c>true</c> 时，此方法在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <param name="unsubscribeOnCapturedContext">是否在捕获的上下文中调用 <paramref name="unsubscribe"/>。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, TEventArgs>> FromEvent<TDelegate, TEventArgs>(
		Func<EventHandler<TEventArgs>, TDelegate> convert, Action<TDelegate> subscribe,
		Action<TDelegate> unsubscribe, CancellationToken cancellationToken, bool unsubscribeOnCapturedContext)
		=> FromAnyEvent<TDelegate, EventArguments<object, TEventArgs>>(
			x => convert((sender, args) => x(CreateEventArguments(sender, args))), subscribe, unsubscribe,
			cancellationToken, unsubscribeOnCapturedContext);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于遵循标准 <c>sender, eventArgs</c> 模式但使用自定义委托类型的事件。
	/// </summary>
	/// <typeparam name="TDelegate">事件委托的类型。</typeparam>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="convert">一个转换委托，接收 <see cref="EventHandler{TEventArgs}"/> 并将其转换为 <typeparamref name="TDelegate"/>。如果显式指定类型参数，则应使用 <c>x => x.Invoke</c>。如果类型参数是推断的，则应使用 <c>(EventHandler&lt;TEventArgs&gt; x) => new TDelegate(x)</c>，并对 <typeparamref name="TEventArgs"/> 和 <typeparamref name="TDelegate"/> 进行相应的替换。</param>
	/// <param name="subscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, TEventArgs>> FromEvent<TDelegate, TEventArgs>(
		Func<EventHandler<TEventArgs>, TDelegate> convert, Action<TDelegate> subscribe,
		Action<TDelegate> unsubscribe, CancellationToken cancellationToken)
		=> FromEvent(convert, subscribe, unsubscribe, cancellationToken, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于遵循标准 <c>sender, eventArgs</c> 模式但使用自定义委托类型的事件。
	/// </summary>
	/// <typeparam name="TDelegate">事件委托的类型。</typeparam>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="convert">一个转换委托，接收 <see cref="EventHandler{TEventArgs}"/> 并将其转换为 <typeparamref name="TDelegate"/>。如果显式指定类型参数，则应使用 <c>x => x.Invoke</c>。如果类型参数是推断的，则应使用 <c>(EventHandler&lt;TEventArgs&gt; x) => new TDelegate(x)</c>，并对 <typeparamref name="TEventArgs"/> 和 <typeparamref name="TDelegate"/> 进行相应的替换。</param>
	/// <param name="subscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <typeparamref name="TDelegate"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<object, TEventArgs>> FromEvent<TDelegate, TEventArgs>(
		Func<EventHandler<TEventArgs>, TDelegate> convert, Action<TDelegate> subscribe,
		Action<TDelegate> unsubscribe)
		=> FromEvent(convert, subscribe, unsubscribe, CancellationToken.None, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action{TSender, TEventArgs}"/> 的事件。
	/// </summary>
	/// <typeparam name="TSender">"发送者"（第一个事件参数）的类型。</typeparam>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="Action{TSender, TEventArgs}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action{TSender, TEventArgs}"/> 并将其从事件中取消订阅。当 <paramref name="unsubscribeOnCapturedContext"/> 为 <c>true</c> 时，此方法在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <param name="unsubscribeOnCapturedContext">是否在捕获的上下文中调用 <paramref name="unsubscribe"/>。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<TSender, TEventArgs>> FromActionEvent<TSender, TEventArgs>(
		Action<Action<TSender, TEventArgs>> subscribe, Action<Action<TSender, TEventArgs>> unsubscribe,
		CancellationToken cancellationToken, bool unsubscribeOnCapturedContext)
		=> FromAnyEvent<Action<TSender, TEventArgs>, EventArguments<TSender, TEventArgs>>(
			x => (sender, args) => x(CreateEventArguments(sender, args)), subscribe, unsubscribe, cancellationToken,
			unsubscribeOnCapturedContext);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action{TSender, TEventArgs}"/> 的事件。
	/// </summary>
	/// <typeparam name="TSender">"发送者"（第一个事件参数）的类型。</typeparam>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="Action{TSender, TEventArgs}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action{TSender, TEventArgs}"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<TSender, TEventArgs>> FromActionEvent<TSender, TEventArgs>(
		Action<Action<TSender, TEventArgs>> subscribe, Action<Action<TSender, TEventArgs>> unsubscribe,
		CancellationToken cancellationToken)
		=> FromActionEvent(subscribe, unsubscribe, cancellationToken, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action{TSender, TEventArgs}"/> 的事件。
	/// </summary>
	/// <typeparam name="TSender">"发送者"（第一个事件参数）的类型。</typeparam>
	/// <typeparam name="TEventArgs">"参数"（第二个事件参数）的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="Action{TSender, TEventArgs}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action{TSender, TEventArgs}"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<EventArguments<TSender, TEventArgs>> FromActionEvent<TSender, TEventArgs>(
		Action<Action<TSender, TEventArgs>> subscribe, Action<Action<TSender, TEventArgs>> unsubscribe)
		=> FromActionEvent(subscribe, unsubscribe, CancellationToken.None, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action{T}"/> 的事件。
	/// </summary>
	/// <typeparam name="TEventArgs">传递给事件处理程序并用于完成任务参数的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="Action{T}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action{T}"/> 并将其从事件中取消订阅。当 <paramref name="unsubscribeOnCapturedContext"/> 为 <c>true</c> 时，此方法在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <param name="unsubscribeOnCapturedContext">是否在捕获的上下文中调用 <paramref name="unsubscribe"/>。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<TEventArgs> FromActionEvent<TEventArgs>(Action<Action<TEventArgs>> subscribe,
															   Action<Action<TEventArgs>> unsubscribe, CancellationToken cancellationToken,
															   bool unsubscribeOnCapturedContext)
		=> FromAnyEvent<Action<TEventArgs>, TEventArgs>(x => x, subscribe, unsubscribe, cancellationToken,
			unsubscribeOnCapturedContext);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action{T}"/> 的事件。
	/// </summary>
	/// <typeparam name="TEventArgs">传递给事件处理程序并用于完成任务的参数的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="Action{T}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action{T}"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<TEventArgs> FromActionEvent<TEventArgs>(Action<Action<TEventArgs>> subscribe,
															   Action<Action<TEventArgs>> unsubscribe, CancellationToken cancellationToken)
		=> FromActionEvent(subscribe, unsubscribe, cancellationToken, true);

	/// <summary>
	/// 返回一个 <see cref="Task{T}"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action{T}"/> 的事件。
	/// </summary>
	/// <typeparam name="TEventArgs">传递给事件处理程序并用于完成任务的参数的类型。</typeparam>
	/// <param name="subscribe">一个方法，接收 <see cref="Action{T}"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action{T}"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task<TEventArgs> FromActionEvent<TEventArgs>(Action<Action<TEventArgs>> subscribe,
															   Action<Action<TEventArgs>> unsubscribe)
		=> FromActionEvent(subscribe, unsubscribe, CancellationToken.None, true);

	/// <summary>
	/// 返回一个 <see cref="Task"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action"/> 的事件。
	/// </summary>
	/// <param name="subscribe">一个方法，接收 <see cref="Action"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action"/> 并将其从事件中取消订阅。当 <paramref name="unsubscribeOnCapturedContext"/> 为 <c>true</c> 时，此方法在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <param name="unsubscribeOnCapturedContext">是否在捕获的上下文中调用 <paramref name="unsubscribe"/>。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task FromActionEvent(Action<Action> subscribe, Action<Action> unsubscribe,
									   CancellationToken cancellationToken, bool unsubscribeOnCapturedContext)
		=> FromAnyEvent<Action, object>(x => () => x(null), subscribe, unsubscribe, cancellationToken,
			unsubscribeOnCapturedContext);

	/// <summary>
	/// 返回一个 <see cref="Task"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action"/> 的事件。
	/// </summary>
	/// <param name="subscribe">一个方法，接收 <see cref="Action"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <param name="cancellationToken">可用于取消任务（并从事件处理程序中取消订阅）的取消令牌。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task FromActionEvent(Action<Action> subscribe, Action<Action> unsubscribe,
									   CancellationToken cancellationToken)
		=> FromActionEvent(subscribe, unsubscribe, cancellationToken, true);

	/// <summary>
	/// 返回一个 <see cref="Task"/>，当指定的事件下一次触发时完成。此重载适用于类型为 <see cref="Action"/> 的事件。
	/// </summary>
	/// <param name="subscribe">一个方法，接收 <see cref="Action"/> 并将其订阅到事件。</param>
	/// <param name="unsubscribe">一个方法，接收 <see cref="Action"/> 并将其从事件中取消订阅。此方法始终在捕获的上下文中调用。</param>
	/// <remarks>
	/// <para>在循环中调用此方法通常是反模式，因为事件仅在此方法被调用时才被订阅，并在任务完成时取消订阅。从任务完成到再次调用此方法之间的时间内，事件可能会触发并"丢失"。如果您发现需要在此方法外包裹循环，请考虑改用 Rx 或 TPL Dataflow。</para>
	/// </remarks>
	public static Task FromActionEvent(Action<Action> subscribe, Action<Action> unsubscribe)
		=> FromActionEvent(subscribe, unsubscribe, CancellationToken.None, true);

	/// <summary>
	/// 创建一个 <see cref="EventArguments{TSender,TEventArgs}"/> 结构。
	/// </summary>
	/// <typeparam name="TSender">事件发送者的类型。</typeparam>
	/// <typeparam name="TEventArgs">事件参数的类型。</typeparam>
	/// <param name="sender">事件的发送者。</param>
	/// <param name="eventArgs">事件参数。</param>
	private static EventArguments<TSender, TEventArgs> CreateEventArguments<TSender, TEventArgs>(TSender sender,
																								 TEventArgs eventArgs)
		=> new()
		{
			Sender = sender,
			EventArgs = eventArgs
		};
}
