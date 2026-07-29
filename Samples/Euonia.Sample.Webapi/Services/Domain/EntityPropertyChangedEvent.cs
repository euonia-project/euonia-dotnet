using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Represents a domain event that signals a single property's value has changed on an entity.
/// Inherit from this class to declare concrete events for specific entity properties (for example,
/// <c>UserNameChangedEvent : EntityPropertyChangedEvent&lt;Guid, string&gt;</c>).
/// </summary>
/// <typeparam name="TKey">The type used to identify the entity (for example <c>Guid</c>, <c>int</c>, or <c>string</c>).</typeparam>
/// <typeparam name="TProperty">The type of the property that changed.</typeparam>
/// <remarks>
/// This class derives from <see cref="DomainEvent"/> to integrate with the domain event handling
/// infrastructure. It is a simple immutable carrier that contains the entity identifier along with
/// the previous and current values of the changed property.
/// </remarks>
public abstract class EntityPropertyChangedEvent<TKey, TProperty> : DomainEvent
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EntityPropertyChangedEvent{TKey, TProperty}"/> class.
	/// </summary>
	/// <param name="id">The identifier of the entity whose property has changed.</param>
	/// <param name="oldValue">The value of the property before the change. May be <c>default</c> for the property type.</param>
	/// <param name="newValue">The value of the property after the change. May be <c>default</c> for the property type.</param>
	protected EntityPropertyChangedEvent(TKey id, TProperty oldValue, TProperty newValue)
	{
		Id = id;
		OldValue = oldValue;
		NewValue = newValue;
	}

	/// <summary>
	/// Gets the identifier of the entity whose property has changed.
	/// </summary>
	/// <value>The entity identifier.</value>
	public TKey Id { get; }

	/// <summary>
	/// Gets the previous value of the changed property, before the update occurred.
	/// </summary>
	/// <value>The property's previous value.</value>
	public TProperty OldValue { get; }

	/// <summary>
	/// Gets the current value of the changed property, after the update occurred.
	/// </summary>
	/// <value>The property's current value.</value>
	public TProperty NewValue { get; }
}
