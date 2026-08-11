namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 表示领域事件的抽象基类。
/// 继承自 <see cref="Event"/> 并实现 <see cref="IDomainEvent"/>。
/// </summary>
/// <seealso cref="Event" />
/// <seealso cref="IDomainEvent" />
public abstract class DomainEvent : Event, IDomainEvent
{
	/// <summary>
	/// 将当前事件附加到指定的事件聚合根上。
	/// </summary>
	/// <typeparam name="TKey">聚合根标识符的类型。</typeparam>
	/// <param name="aggregate">要附加到的聚合根。</param>
	public void Attach<TKey>(IAggregateRoot<TKey> aggregate)
		where TKey : IEquatable<TKey>
	{
		OriginatorId = aggregate.Id?.ToString();
		OriginatorType = aggregate.GetType().AssemblyQualifiedName;
		AggregatePayload = aggregate;
	}

	/// <summary>
	/// 获取事件聚合。
	/// </summary>
	/// <returns>事件聚合实例。</returns>
	public virtual EventAggregate GetEventAggregate()
	{
		return new EventAggregate
		{
			Id = ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			TypeName = GetType().AssemblyQualifiedName,
			EventIntent = EventIntent,
			Timestamp = DateTime.UtcNow,
			OriginatorId = OriginatorId,
			OriginatorType = OriginatorType,
			EventSequence = Sequence,
			EventPayload = this
		};
	}

	/// <summary>
	/// 获取或设置聚合负载。
	/// </summary>
	/// <value>聚合负载。</value>
	public virtual object AggregatePayload { get; set; }

	/// <summary>
	/// 获取已附加的聚合根对象。
	/// </summary>
	/// <typeparam name="TAggregate">聚合根的类型。</typeparam>
	/// <returns>附加的聚合根实例；若不存在或类型不匹配则返回默认值。</returns>
	public virtual TAggregate GetAggregate<TAggregate>()
		where TAggregate : IAggregateRoot
	{
		return AggregatePayload switch
		{
			null => default,
			TAggregate aggregate => aggregate,
			_ => default,
		};
	}
}