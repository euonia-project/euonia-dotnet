namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 为支持更改跟踪、状态管理和属性授权的业务对象提供基类。
/// 支持将对象标记为新增、已更改或已删除，并管理编辑工作流中的繁忙和可保存状态。
/// </summary>
/// <remarks>此类实现可操作属性和可编辑对象的接口，允许对对象状态和属性更改进行细粒度控制。
/// 当繁忙状态改变时引发事件，并提供检查和更新对象状态的方法。
/// 将此类作为需要跟踪编辑、授权和验证的业务对象的基础。</remarks>
/// <typeparam name="T">派生可观察对象的类型。必须继承自 ObservableObject{T}。</typeparam>
public abstract class ObservableObject<T> : BusinessObject<T>, IOperableProperty, IEditableObject
	where T : ObservableObject<T>
{
	/// <summary>
	/// 获取当前的对象状态。
	/// </summary>
	public ObjectEditState State { get; private set; } = ObjectEditState.None;

	/// <summary>
	/// 获取一个值，指示对象是否为新增。
	/// </summary>
	public bool IsNew => State == ObjectEditState.New;

	/// <summary>
	/// 获取一个值，指示对象是否已更改。
	/// </summary>
	public bool IsChanged => State == ObjectEditState.Changed;

	/// <summary>
	/// 获取一个值，指示对象是否将被删除。
	/// </summary>
	public bool IsDeleted => State == ObjectEditState.Deleted;

	/// <summary>
	/// 获取或设置一个值，指示删除时是否检查对象规则。
	/// </summary>
	public bool CheckObjectRulesOnDelete { get; private set; }

	/// <summary>
	/// 将对象状态标记为 <see cref="ObjectEditState.New"/>。
	/// </summary>
	public void MarkAsNew()
	{
		State = ObjectEditState.New;
	}

	/// <summary>
	/// 将对象状态标记为 <see cref="ObjectEditState.Changed"/>。
	/// </summary>
	public void MarkAsChanged()
	{
		State = ObjectEditState.Changed;
	}

	/// <summary>
	/// 将对象状态标记为 <see cref="ObjectEditState.Deleted"/>。
	/// </summary>
	/// <param name="checkObjectRules">是否在删除时检查对象规则。</param>
	public void MarkAsDeleted(bool checkObjectRules = false)
	{
		State = ObjectEditState.Deleted;
		CheckObjectRulesOnDelete = checkObjectRules;
	}

	/// <summary>
	/// 提交当前更改，将对象状态重置为 <see cref="ObjectEditState.None"/>。
	/// </summary>
	/// <remarks>
	/// 在清除已更改属性跟踪与字段修改历史（由 <see cref="ObjectBase.AcceptChanges"/> 完成）的同时，
	/// 将对象状态重置为 <see cref="ObjectEditState.None"/> 并清除删除时的对象规则检查标记，
	/// 令 <see cref="IsChanged"/> 返回 <see langword="false"/>。
	/// </remarks>
	public override void AcceptChanges()
	{
		base.AcceptChanges();
		State = ObjectEditState.None;
		CheckObjectRulesOnDelete = false;
	}

	/// <summary>
	/// 将对象标记为干净状态：提交当前更改，清空更改跟踪与字段修改历史，并将对象状态重置为
	/// <see cref="ObjectEditState.None"/>。
	/// </summary>
	/// <remarks>
	/// 通常在对象被成功保存或加载后调用，使 <see cref="IsChanged"/>、<see cref="BusinessObject{T}.HasChangedProperties"/> 和
	/// <see cref="IsSavable"/> 恢复初始状态。对象状态非 <see cref="ObjectEditState.None"/> 时也可显式调用，
	/// 用于放弃未保存的更改。
	/// </remarks>
	public virtual void MarkAsClean()
	{
		AcceptChanges();
	}

	/// <summary>
	/// 用于跟踪繁忙状态的计数器。
	/// </summary>
	private int _isBusyCounter;

	/// <summary>
	/// 获取一个值，指示对象是否繁忙。
	/// </summary>
	public virtual bool IsBusy => IsSelfBusy || (FieldManagerOrNull?.IsBusy() ?? false);

	/// <summary>
	/// 获取一个值，指示对象本身是否繁忙。
	/// </summary>
	public virtual bool IsSelfBusy => _isBusyCounter > 0 || HasRunningRules;

	/// <summary>
	/// 获取一个值，指示对象是否可保存。
	/// </summary>
	public virtual bool IsSavable => IsValid && (HasChangedProperties || IsChanged) && !IsBusy;

	/// <summary>
	/// 当繁忙状态改变时引发的事件。
	/// </summary>
	/// <remarks>
	/// 字段式事件：订阅/退订由编译器生成的 <c>Interlocked.CompareExchange</c> 循环保证原子，
	/// 并发订阅/退订不会静默丢失处理器。
	/// </remarks>
	public event BusyChangedEventHandler BusyChanged;

	/// <summary>
	/// 引发 <see cref="BusyChanged"/> 事件。
	/// </summary>
	/// <param name="args">事件参数。</param>
	protected virtual void OnBusyChanged(BusyChangedEventArgs args)
	{
		BusyChanged?.Invoke(this, args);
	}

	/// <summary>
	/// 将对象标记为繁忙。
	/// </summary>
	protected virtual void MarkAsBusy()
	{
		var updatedValue = Interlocked.Increment(ref _isBusyCounter);

		if (updatedValue == 1 && BusyChanged != null)
		{
			OnBusyChanged(new BusyChangedEventArgs(string.Empty, true));
		}
	}

	/// <summary>
	/// 将对象标记为空闲。
	/// </summary>
	protected virtual void MarkAsIdle()
	{
		var updatedValue = Interlocked.Decrement(ref _isBusyCounter);
		switch (updatedValue)
		{
			case < 0:
				_ = Interlocked.CompareExchange(ref _isBusyCounter, 0, updatedValue);
				break;
			case 0:
				if (BusyChanged != null)
				{
					OnBusyChanged(new BusyChangedEventArgs("", false));
				}

				break;
		}
	}
}
