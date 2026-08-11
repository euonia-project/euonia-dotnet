namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 表示领域事件的接口。
/// 继承自 <see cref="IEvent"/>。
/// </summary>
/// <seealso cref="IEvent" />
public interface IDomainEvent : IEvent
{
	/// <summary>
	/// 将当前事件附加到指定的事件聚合根上。
	/// </summary>
	/// <param name="aggregate">要附加到的聚合根。</param>
	/// <typeparam name="TKey">聚合根标识符的类型。</typeparam>
	void Attach<TKey>(IAggregateRoot<TKey> aggregate)
		where TKey : IEquatable<TKey>;

	/// <summary>
	/// 获取事件聚合。
	/// </summary>
	/// <returns>事件聚合实例。</returns>
	EventAggregate GetEventAggregate();
}