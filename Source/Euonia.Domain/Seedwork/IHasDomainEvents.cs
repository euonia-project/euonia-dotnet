namespace Nerosoft.Euonia.Domain;

/// <summary>
/// Represents the object has domain events.
/// </summary>
public interface IHasDomainEvents
{
	/// <summary>
	/// Gets the attached domain events.
	/// </summary>
	/// <returns></returns>
	IEnumerable<DomainEvent> GetEvents();

	/// <summary>
	/// Raise up a new event.
	/// </summary>
	/// <param name="event"></param>
	/// <typeparam name="TEvent"></typeparam>
	void RaiseEvent<TEvent>(TEvent @event)
		where TEvent : DomainEvent;

	/// <summary>
	/// Apply the specific event.
	/// </summary>
	/// <typeparam name="TEvent"></typeparam>
	/// <param name="event"></param>
	void Apply<TEvent>(TEvent @event)
		where TEvent : DomainEvent;

	/// <summary>
	/// Clear events.
	/// </summary>
	void ClearEvents();

	/// <summary>
	/// Attach to events.
	/// </summary>
	void AttachToEvents();
}