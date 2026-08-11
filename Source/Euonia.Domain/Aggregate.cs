namespace Nerosoft.Euonia.Domain;

/// <summary>
/// <see cref="IAggregateRoot{TKey}"/> 的抽象实现。
/// </summary>
/// <typeparam name="TKey">标识符类型。</typeparam>
public abstract class Aggregate<TKey> : Entity<TKey>, IAggregateRoot<TKey>, IHasDomainEvents
	where TKey : IEquatable<TKey>
{
	/// <summary>
	/// 存储事件类型与其处理器的映射。
	/// </summary>
	private readonly Dictionary<Type, Action<object>> _handlers = new();

	/// <summary>
	/// 存储聚合引发的事件列表。
	/// </summary>
	private readonly List<DomainEvent> _events = [];

	/// <summary>
	/// 获取事件集合。
	/// </summary>
	public virtual IReadOnlyList<DomainEvent> GetEvents() => _events?.AsReadOnly();

	/// <summary>
	/// 为特定事件类型注册处理器。
	/// </summary>
	/// <typeparam name="T">事件的类型。</typeparam>
	/// <param name="when">处理事件的委托。</param>
	protected virtual void Register<T>(Action<T> when)
	{
		_handlers.Add(typeof(T), @event => when((T)@event));
	}

	/// <summary>
	/// 引发一个新事件。
	/// </summary>
	/// <param name="event">要引发的事件。</param>
	public virtual void RaiseEvent<TEvent>(TEvent @event)
		where TEvent : DomainEvent
	{
		if (_handlers.TryGetValue(typeof(TEvent), out var handler))
		{
			handler(@event);
		}

		_events.Add(@event);
	}

	/// <summary>
	/// 应用指定的事件，调用已注册的处理器。
	/// </summary>
	/// <typeparam name="TEvent">事件的类型。</typeparam>
	/// <param name="event">要应用的事件。</param>
	public virtual void Apply<TEvent>(TEvent @event)
		where TEvent : DomainEvent
	{
		if (_handlers.TryGetValue(typeof(TEvent), out var handler))
		{
			handler(@event);
		}
	}

	/// <summary>
	/// 清除事件。
	/// </summary>
	public virtual void ClearEvents()
	{
		_events.Clear();
	}

	/// <summary>
	/// 将所有事件附加到当前聚合上。
	/// </summary>
	public virtual void AttachToEvents()
	{
		foreach (var @event in _events)
		{
			@event.Attach(this);
		}
	}
}