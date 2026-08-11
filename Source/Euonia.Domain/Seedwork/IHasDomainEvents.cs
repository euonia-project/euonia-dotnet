namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 表示对象具有领域事件。
/// </summary>
public interface IHasDomainEvents
{
	/// <summary>
	/// 获取已附加的领域事件。
	/// </summary>
	/// <returns>已附加的领域事件集合。</returns>
	IReadOnlyList<DomainEvent> GetEvents();

	/// <summary>
	/// 引发一个新事件。
	/// </summary>
	/// <param name="event">要引发的事件。</param>
	/// <typeparam name="TEvent">事件的类型。</typeparam>
	void RaiseEvent<TEvent>(TEvent @event)
		where TEvent : DomainEvent;

	/// <summary>
	/// 应用指定的事件。
	/// </summary>
	/// <typeparam name="TEvent">事件的类型。</typeparam>
	/// <param name="event">要应用的事件。</param>
	void Apply<TEvent>(TEvent @event)
		where TEvent : DomainEvent;

	/// <summary>
	/// 清除事件。
	/// </summary>
	void ClearEvents();

	/// <summary>
	/// 附加到事件。
	/// </summary>
	void AttachToEvents();
}