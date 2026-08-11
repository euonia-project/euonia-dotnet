using Nerosoft.Euonia.Reflection;

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
	/// 用于跟踪繁忙状态的计数器。
	/// </summary>
	private int _isBusyCounter;

	/// <summary>
	/// 获取一个值，指示对象是否繁忙。
	/// </summary>
	public virtual bool IsBusy => IsSelfBusy || (FieldManager != null && FieldManager.IsBusy());

	/// <summary>
	/// 获取一个值，指示对象本身是否繁忙。
	/// </summary>
	public virtual bool IsSelfBusy => _isBusyCounter > 0 || Rules.HasRunningRules;

	/// <summary>
	/// 获取一个值，指示对象是否可保存。
	/// </summary>
	public virtual bool IsSavable => IsValid && (HasChangedProperties || IsChanged) && !IsBusy;

	private BusyChangedEventHandler _busyChanged;

	/// <summary>
	/// 当繁忙状态改变时引发的事件。
	/// </summary>
	public event BusyChangedEventHandler BusyChanged
	{
		// add => _busyChanged += value;
		// remove => _busyChanged -= value;
		add => _busyChanged = (BusyChangedEventHandler)Delegate.Combine(_busyChanged, value);
		remove => _busyChanged = (BusyChangedEventHandler)Delegate.Remove(_busyChanged, value);
	}

	/// <summary>
	/// 引发 <see cref="BusyChanged"/> 事件。
	/// </summary>
	/// <param name="args">事件参数。</param>
	protected virtual void OnBusyChanged(BusyChangedEventArgs args)
	{
		_busyChanged?.Invoke(this, args);
	}

	/// <summary>
	/// 将对象标记为繁忙。
	/// </summary>
	protected virtual void MarkAsBusy()
	{
		var updatedValue = Interlocked.Increment(ref _isBusyCounter);

		if (updatedValue == 1)
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
				OnBusyChanged(new BusyChangedEventArgs("", false));
				break;
		}
	}

	#region Get Properties

	/// <summary>
	/// 获取属性值，首先检查授权。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="field">当前字段值。</param>
	/// <param name="defaultValue">默认值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>属性值。</returns>
	protected virtual TValue GetProperty<TValue>(string propertyName, TValue field, TValue defaultValue)
	{
		var propertyInfo = FieldManager.GetRegisteredProperty(propertyName);

		if (IsBypassingRuleChecks || CanReadProperty(propertyInfo, true))
		{
			return field;
		}

		return defaultValue;
	}

	/// <summary>
	/// 获取属性值，首先检查授权。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="field">当前字段值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>属性值。</returns>
	protected TValue GetProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue field)
	{
		return GetProperty(propertyInfo.Name, field, propertyInfo.DefaultValue);
	}

	/// <summary>
	/// 获取属性值，首先检查授权。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="field">当前字段值。</param>
	/// <param name="defaultValue">默认值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>属性值。</returns>
	protected TValue GetProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue field, TValue defaultValue)
	{
		return GetProperty(propertyInfo.Name, field, defaultValue);
	}

	/// <summary>
	/// 获取属性值，首先检查授权。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="field">当前字段值。</param>
	/// <typeparam name="TField">字段的类型。</typeparam>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>转换后的属性值。</returns>
	protected TValue GetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo, TField field)
	{
		return TypeHelper.CoerceValue<TValue>(typeof(TField), GetProperty(propertyInfo.Name, field, propertyInfo.DefaultValue));
	}

	/// <summary>
	/// 获取属性值，首先检查授权。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <typeparam name="TField">字段的类型。</typeparam>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>转换后的属性值。</returns>
	protected TValue GetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo)
	{
		return TypeHelper.CoerceValue<TValue>(typeof(TField), GetProperty(propertyInfo));
	}

	/// <summary>
	/// 获取属性值，首先检查授权。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>属性值。</returns>
	protected TValue GetProperty<TValue>(PropertyInfo<TValue> propertyInfo)
	{
		TValue result;
		if (IsBypassingRuleChecks || CanReadProperty(propertyInfo, true))
			result = ReadProperty(propertyInfo);
		else
			result = propertyInfo.DefaultValue;
		return result;
	}

	/// <summary>
	/// 获取 <see cref="IPropertyInfo"/> 属性的值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <returns>属性值。</returns>
	public object GetProperty(IPropertyInfo propertyInfo)
	{
		object result;
		if (IsBypassingRuleChecks || CanReadProperty(propertyInfo, false))
		{
			// 调用 ReadProperty（可能在实际类中被重载）
			result = ReadProperty(propertyInfo);
		}
		else
		{
			result = propertyInfo.DefaultValue;
		}

		return result;
	}

	/// <summary>
	/// 获取 <see cref="IPropertyInfo"/> 属性的值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>属性值。</returns>
	protected TValue GetProperty<TValue>(IPropertyInfo propertyInfo)
	{
		return (TValue)GetProperty(propertyInfo);
	}

	#endregion

	#region Set Properties

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <remarks>
	///	此重载将属性赋值操作委托给按属性名称设置的重载。
	/// 设置前会检查写入权限，并在值发生变化时触发属性更改通知。
	/// </remarks>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="field">字段引用。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected virtual void SetProperty<TValue>(PropertyInfo<TValue> propertyInfo, ref TValue field, TValue newValue)
	{
		SetProperty(propertyInfo.Name, ref field, newValue);
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="field">字段引用。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TField">字段的类型。</typeparam>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected virtual void SetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo, ref TField field, TValue newValue)
	{
		SetPropertyConvert(propertyInfo.Name, ref field, newValue);
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <remarks>
	///	此方法按属性名称执行属性赋值：先检查写入权限，再比较新旧值。
	/// 仅在值发生变化时触发属性更改通知；当规则检查被忽略（IsBypassingRuleChecks）时不触发任何通知。
	/// </remarks>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="field">字段引用。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected virtual void SetProperty<TValue>(string propertyName, ref TValue field, TValue newValue)
	{
		var propertyInfo = FieldManager.GetRegisteredProperty(propertyName);

		if (!IsBypassingRuleChecks && !CanWriteProperty(propertyInfo, true))
		{
			return;
		}

		var doChange = false;
		if (field == null)
		{
			doChange = newValue != null;
		}
		else
		{
			if (typeof(TValue) == typeof(string) && newValue == null)
			{
				newValue = TypeHelper.CoerceValue<TValue>(typeof(string), string.Empty);
			}

			if (ValuesDiffer(propertyInfo, newValue, field))
			{
				doChange = true;
			}
		}

		if (!doChange)
		{
			return;
		}

		if (!IsBypassingRuleChecks)
		{
			OnPropertyChanging(propertyName);
		}

		field = newValue;
		if (!IsBypassingRuleChecks)
		{
			PropertyHasChanged(propertyName);
		}
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="field">字段引用。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TField">字段的类型。</typeparam>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected virtual void SetPropertyConvert<TField, TValue>(string propertyName, ref TField field, TValue newValue)
	{
		#region Check to see if the property is marked with RelationshipTypes.PrivateField

		var propertyInfo = FieldManager.GetRegisteredProperty(propertyName);

		#endregion

		if (!IsBypassingRuleChecks && !CanWriteProperty(propertyInfo, true))
		{
			return;
		}

		var doChange = false;
		if (field == null)
		{
			if (newValue != null)
			{
				doChange = true;
			}
		}
		else
		{
			if (typeof(TValue) == typeof(string) && newValue == null)
			{
				newValue = TypeHelper.CoerceValue<TValue>(typeof(string), string.Empty);
			}

			if (!field.Equals(newValue))
			{
				doChange = true;
			}
		}

		if (doChange)
		{
			if (!IsBypassingRuleChecks)
			{
				OnPropertyChanging(propertyName);
			}

			field = TypeHelper.CoerceValue<TField>(typeof(TValue), newValue);
			if (!IsBypassingRuleChecks)
			{
				PropertyHasChanged(propertyName);
			}
		}
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="newValue">新值。</param>
	/// <param name="onChanged">值更改时的回调操作。</param>
	/// <typeparam name="TField">字段的类型。</typeparam>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected virtual void SetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo, TValue newValue, Action<IPropertyInfo, TField, TField> onChanged = null)
	{
		if (!IsBypassingRuleChecks && !CanWriteProperty(propertyInfo, true))
		{
			return;
		}

		TField oldValue;
		var fieldData = FieldManager.GetFieldData(propertyInfo);
		switch (fieldData)
		{
			case null:
				oldValue = propertyInfo.DefaultValue;
				var _ = FieldManager.LoadFieldData(propertyInfo, oldValue);
				break;
			case IFieldData<TField> fd:
				oldValue = fd.Value;
				break;
			default:
				oldValue = (TField)fieldData.Value;
				break;
		}

		if (typeof(TValue) == typeof(string) && newValue == null)
		{
			newValue = TypeHelper.CoerceValue<TValue>(typeof(string), string.Empty);
		}

		LoadPropertyValue(propertyInfo, oldValue, TypeHelper.CoerceValue<TField>(typeof(TValue), newValue), !IsBypassingRuleChecks, onChanged);
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <remarks>
	///	此方法在设置属性值之前会检查是否有写入权限。
	/// 在不忽略规则检查的情况下，调用此方法会对新值进行规则检查，并在值更改时触发回调操作。
	/// </remarks>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="newValue">新值。</param>
	/// <param name="onChanged">值更改时的回调操作。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected virtual void SetProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue, Action<IPropertyInfo, TValue, TValue> onChanged = null)
	{
		if (!IsBypassingRuleChecks && !CanWriteProperty(propertyInfo, true))
		{
			return;
		}

		TValue oldValue;
		var fieldData = FieldManager.GetFieldData(propertyInfo);
		switch (fieldData)
		{
			case null:
				oldValue = propertyInfo.DefaultValue;
				var _ = FieldManager.LoadFieldData(propertyInfo, oldValue);
				break;
			case IFieldData<TValue> fd:
				oldValue = fd.Value;
				break;
			default:
				oldValue = (TValue)fieldData.Value;
				break;
		}

		if (typeof(TValue) == typeof(string) && newValue == null)
		{
			newValue = TypeHelper.CoerceValue<TValue>(typeof(string), string.Empty);
		}

		LoadPropertyValue(propertyInfo, oldValue, newValue, !IsBypassingRuleChecks, onChanged);
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <remarks>
	///	这是 IOperableProperty 接口的实现入口。设置前检查写入权限；
	/// 在规则检查未被忽略时，先触发 OnPropertyChanging，写入字段数据后再触发属性更改通知。
	/// </remarks>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="newValue">新值。</param>
	public void SetProperty(IPropertyInfo propertyInfo, object newValue)
	{
		if (!IsBypassingRuleChecks && !CanWriteProperty(propertyInfo, true))
		{
			return;
		}

		if (!IsBypassingRuleChecks)
		{
			OnPropertyChanging(propertyInfo);
		}

		FieldManager.SetFieldData(propertyInfo, newValue);

		if (!IsBypassingRuleChecks)
		{
			PropertyHasChanged(propertyInfo);
		}
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <remarks>
	///	此强类型重载将赋值委托给非泛型重载 SetProperty(IPropertyInfo, object)。
	/// 设置前会检查写入权限，并在规则检查未被忽略时触发属性更改通知。
	/// </remarks>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected virtual void SetProperty<TValue>(IPropertyInfo propertyInfo, TValue newValue)
	{
		SetProperty(propertyInfo, (object)newValue);
	}

	#endregion
}