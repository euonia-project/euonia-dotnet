using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Base class for editable business objects that supports lazy service resolution.
/// </summary>
/// <typeparam name="TTarget">The concrete editable object type that derives from this base class.</typeparam>
/// <typeparam name="TKey">The object identifier type.</typeparam>
public abstract class EditableObjectBase<TTarget, TKey> : EditableObject<TTarget>, IAggregateRoot<TKey>, IHasDomainEvents
	where TTarget : EditableObjectBase<TTarget, TKey>
	where TKey : IEquatable<TKey>
{
	public static readonly PropertyInfo<TKey> IdProperty = RegisterProperty<TKey>(p => p.Id);

	public virtual TKey Id
	{
		get => GetProperty(IdProperty);
		set => LoadProperty(IdProperty, value);
	}

	private readonly Dictionary<Type, Action<object>> _handlers = new();

	private readonly List<DomainEvent> _events = [];

	/// <summary>
	/// The events.
	/// </summary>
	public virtual IReadOnlyList<DomainEvent> GetEvents() => _events?.AsReadOnly();

	/// <summary>
	/// Register a handler for the specific event type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="when"></param>
	protected virtual void Register<T>(Action<T> when)
	{
		_handlers.Add(typeof(T), @event => when((T)@event));
	}

	/// <summary>
	/// Raise up a new event.
	/// </summary>
	/// <param name="event"></param>
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
	/// 
	/// </summary>
	/// <typeparam name="TEvent"></typeparam>
	/// <param name="event"></param>
	public virtual void Apply<TEvent>(TEvent @event)
		where TEvent : DomainEvent
	{
		if (_handlers.TryGetValue(typeof(TEvent), out var handler))
		{
			handler(@event);
		}
	}

	/// <summary>
	/// Clear events.
	/// </summary>
	public virtual void ClearEvents()
	{
		_events.Clear();
	}

	/// <summary>
	/// 
	/// </summary>
	public virtual void AttachToEvents()
	{
		foreach (var @event in _events)
		{
			@event.Attach(this);
		}
	}

	/// <summary>
	/// Returns an array of ordered keys for this entity.
	/// </summary>
	/// <returns></returns>
	public virtual object[] GetKeys() => [Id!];
}