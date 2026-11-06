using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// 支持延迟解析服务的可编辑业务对象基类。
/// </summary>
/// <typeparam name="TTarget">派生自此基类的具体可编辑对象类型。</typeparam>
/// <typeparam name="TKey">对象标识类型。</typeparam>
public abstract class EditableObjectBase<TTarget, TKey> : EditableObject<TTarget>, IAggregateRoot<TKey>, IHasDomainEvents
	where TTarget : EditableObjectBase<TTarget, TKey>
	where TKey : IEquatable<TKey>
{
	/// <summary>
	/// 注册的标识属性元数据，供属性读写与工厂按标识定位对象时使用。
	/// </summary>
	public static readonly PropertyInfo<TKey> IdProperty = RegisterProperty<TKey>(p => p.Id);

	/// <summary>
	/// 获取或设置对象标识；写入直接落到属性存储，不标记为已修改。
	/// </summary>
	public virtual TKey Id
	{
		get => GetProperty(IdProperty);
		set => LoadProperty(IdProperty, value);
	}

	private readonly Dictionary<Type, Action<object>> _handlers = new();

	private readonly List<DomainEvent> _events = [];

	/// <summary>
	/// 事件集合。
	/// </summary>
	public virtual IReadOnlyList<DomainEvent> GetEvents() => _events.AsReadOnly();

	/// <summary>
	/// 为特定事件类型注册处理程序。
	/// </summary>
	/// <typeparam name="T">要处理的事件类型。</typeparam>
	/// <param name="when">事件触发时执行的处理委托。</param>
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
	/// 应用事件，仅调用已注册的处理程序而不将其加入事件集合。
	/// </summary>
	/// <typeparam name="TEvent">事件类型。</typeparam>
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
	/// 将当前对象附加到所有已记录的事件上。
	/// </summary>
	public virtual void AttachToEvents()
	{
		foreach (var @event in _events)
		{
			@event.Attach(this);
		}
	}

	/// <summary>
	/// 返回此实体的有序键数组。
	/// </summary>
	/// <returns>包含当前对象标识的有序键数组。</returns>
	public virtual object[] GetKeys() => [Id!];
}