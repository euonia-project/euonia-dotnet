using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// 表示实体上单个属性的值已发生变化的领域事件。
/// 从本类派生即可为特定实体属性声明具体事件（例如
/// <c>UserNameChangedEvent : EntityPropertyChangedEvent&lt;Guid, string&gt;</c>）。
/// </summary>
/// <typeparam name="TKey">用于标识实体的类型（例如 <c>Guid</c>、<c>int</c> 或 <c>string</c>）。</typeparam>
/// <typeparam name="TProperty">发生变化的属性类型。</typeparam>
/// <remarks>
/// 本类派生自 <see cref="DomainEvent"/>，以便与领域事件处理基础设施集成。
/// 它是一个简单的不可变载体，包含实体标识以及所变更属性的旧值与新值。
/// </remarks>
public abstract class EntityPropertyChangedEvent<TKey, TProperty> : DomainEvent
{
	/// <summary>
	/// 初始化 <see cref="EntityPropertyChangedEvent{TKey, TProperty}"/> 类的新实例。
	/// </summary>
	/// <param name="id">属性发生变化的实体的标识。</param>
	/// <param name="oldValue">变更前的属性值。可能为该属性类型的 <c>default</c>。</param>
	/// <param name="newValue">变更后的属性值。可能为该属性类型的 <c>default</c>。</param>
	protected EntityPropertyChangedEvent(TKey id, TProperty oldValue, TProperty newValue)
	{
		Id = id;
		OldValue = oldValue;
		NewValue = newValue;
	}

	/// <summary>
	/// 获取属性发生变化的实体的标识。
	/// </summary>
	/// <value>实体标识。</value>
	public TKey Id { get; }

	/// <summary>
	/// 获取发生变更的属性在更新前的旧值。
	/// </summary>
	/// <value>属性的旧值。</value>
	public TProperty OldValue { get; }

	/// <summary>
	/// 获取发生变更的属性在更新后的当前值。
	/// </summary>
	/// <value>属性的当前值。</value>
	public TProperty NewValue { get; }
}
