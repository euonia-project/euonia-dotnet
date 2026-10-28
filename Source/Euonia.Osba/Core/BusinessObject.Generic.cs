using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 带变更追踪的业务对象基类。
/// </summary>
/// <remarks>
/// <para>
/// 变更追踪（已更改属性、脏状态、<see cref="AcceptChanges"/>）只属于<b>需要编辑/保存生命周期的对象</b>：
/// <see cref="ObservableObject{T}"/>、<see cref="EditableObject{T}"/>、<see cref="ReadOnlyObject{T}"/>。
/// </para>
/// <para>
/// <see cref="ServiceObject{T}"/> 直接继承 <see cref="ObjectBase{T}"/>，只保留属性与规则能力，
/// 不继承本类，因此没有 <see cref="ChangedProperties"/> / <see cref="HasChangedProperties"/> 的脏语义。
/// </para>
/// </remarks>
/// <typeparam name="T">业务对象的具体类型。</typeparam>
public abstract class BusinessObject<T> : ObjectBase<T>, IBusinessObject
	where T : BusinessObject<T>
{
	/// <summary>
	/// 已更改属性的列表。
	/// </summary>
	private readonly List<IPropertyInfo> _changedProperties = [];

	/// <summary>
	/// 已更改属性的只读快照；集合变化时失效。
	/// </summary>
	private IReadOnlyList<IPropertyInfo> _changedPropertiesSnapshot;

	/// <summary>
	/// 保护 <see cref="_changedProperties"/> 并发访问的同步锁。
	/// </summary>
	private readonly Lock _changedPropertiesLock = new();

	/// <summary>
	/// 将指定属性标记为脏的或已更改。
	/// </summary>
	/// <param name="property">要标记的属性信息。</param>
	protected override void PropertyHasChanged(IPropertyInfo property)
	{
		lock (_changedPropertiesLock)
		{
			if (!_changedProperties.Contains(property))
			{
				_changedProperties.Add(property);
				_changedPropertiesSnapshot = null;
			}
		}

		// 该属性没有规则时退化为普通变更通知——绑定依赖这条通知，
		// 不能因为「进了规则分支但无规则可跑」而丢掉它。
		if (CheckRuleOnPropertyChanged && HasAnyRulesFor(property))
		{
			CheckPropertyRules(property);
			SetValidationState(IsValid ? ValidationState.NotValidated : ValidationState.Invalid);
		}
		else
		{
			SetValidationState(ValidationState.NotValidated);
			OnPropertyChanged(property);
		}
	}

	/// <summary>
	/// 获取保存前需要补跑属性级规则的属性集合。
	/// </summary>
	protected override IReadOnlyList<IPropertyInfo> RulesPropertiesToCheck => ChangedProperties;

	/// <summary>
	/// 获取已更改属性的列表快照，可安全跨线程读取。
	/// </summary>
	public virtual IReadOnlyList<IPropertyInfo> ChangedProperties
	{
		get
		{
			var snapshot = Volatile.Read(ref _changedPropertiesSnapshot);
			if (snapshot != null)
			{
				return snapshot;
			}

			lock (_changedPropertiesLock)
			{
				if (_changedPropertiesSnapshot == null)
				{
					_changedPropertiesSnapshot = _changedProperties.Count == 0
						                             ? Array.Empty<IPropertyInfo>()
						                             : [.. _changedProperties];
				}

				return _changedPropertiesSnapshot;
			}
		}
	}

	/// <summary>
	/// 检查对象是否具有已更改的属性。
	/// </summary>
	public virtual bool HasChangedProperties
	{
		get
		{
			lock (_changedPropertiesLock)
			{
				return _changedProperties.Count > 0;
			}
		}
	}

	/// <summary>
	/// 提交当前更改：清除所有已更改属性的跟踪记录，并使字段的修改历史失效，
	/// 将字段当前值作为新的基线。
	/// </summary>
	/// <remarks>
	/// 通常在对象被成功保存或加载后调用，使对象恢复“干净”状态。
	/// </remarks>
	public override void AcceptChanges()
	{
		lock (_changedPropertiesLock)
		{
			_changedProperties.Clear();
			_changedPropertiesSnapshot = Array.Empty<IPropertyInfo>();
		}

		base.AcceptChanges();
	}

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
		return IsBypassingRuleChecks || CanReadProperty(propertyInfo, true) ? field : propertyInfo.DefaultValue;
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
		return IsBypassingRuleChecks || CanReadProperty(propertyInfo, true) ? field : defaultValue;
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
		return TypeHelper.CoerceValue<TValue>(typeof(TField), GetProperty(propertyInfo, field, propertyInfo.DefaultValue));
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
		if (!IsBypassingRuleChecks && !CanWriteProperty(propertyInfo, true))
		{
			return;
		}

		if (typeof(TValue) == typeof(string) && newValue == null)
		{
			newValue = TypeHelper.CoerceValue<TValue>(typeof(string), string.Empty);
		}

		if (!ValuesDiffer(propertyInfo, newValue, field))
		{
			return;
		}

		if (!IsBypassingRuleChecks)
		{
			OnPropertyChanging(propertyInfo);
		}

		AttachChild(propertyInfo, field, newValue);
		field = newValue;

		// 字段模式与字典模式共用同一份“可被规则读取”的值：
		// 规则/DataAnnotations 走 ReadProperty（字典），这里同步一份，避免它们读到默认值。
		FieldManager.SetFieldData(propertyInfo, newValue);

		if (!IsBypassingRuleChecks)
		{
			PropertyHasChanged(propertyInfo);
		}
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
		if (!IsBypassingRuleChecks && !CanWriteProperty(propertyInfo, true))
		{
			return;
		}

		if (typeof(TValue) == typeof(string) && newValue == null)
		{
			newValue = TypeHelper.CoerceValue<TValue>(typeof(string), string.Empty);
		}

		var converted = TypeHelper.CoerceValue<TField>(typeof(TValue), newValue);
		if (EqualityComparer<TField>.Default.Equals(field, converted))
		{
			return;
		}

		if (!IsBypassingRuleChecks)
		{
			OnPropertyChanging(propertyInfo);
		}

		AttachChild(propertyInfo, field, converted);
		field = converted;
		FieldManager.SetFieldData(propertyInfo, converted);

		if (!IsBypassingRuleChecks)
		{
			PropertyHasChanged(propertyInfo);
		}
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

		AttachChild(propertyInfo, field, newValue);
		field = newValue;
		FieldManager.SetFieldData(propertyInfo, newValue);
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

		var propertyInfo = FieldManager.GetRegisteredProperty(propertyName);


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

			var converted = TypeHelper.CoerceValue<TField>(typeof(TValue), newValue);
			AttachChild(propertyInfo, field, converted);
			field = converted;
			FieldManager.SetFieldData(propertyInfo, field);
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

		var oldValue = FieldManager.GetExistingOrInit(propertyInfo);

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

		var oldValue = FieldManager.GetExistingOrInit(propertyInfo);

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

		if (propertyInfo.Type == typeof(string) && newValue == null)
		{
			newValue = string.Empty;
		}

		// 与泛型入口保持一致：赋同一个值不算变更，不标脏、不触发规则、不发出通知。
		var currentValue = ReadProperty(propertyInfo);
		if (!ValuesDiffer(propertyInfo, newValue, currentValue))
		{
			return;
		}

		if (!IsBypassingRuleChecks)
		{
			OnPropertyChanging(propertyInfo);
		}

		AttachChild(propertyInfo, currentValue, newValue);
		FieldManager.SetFieldData(propertyInfo, newValue);

		if (!IsBypassingRuleChecks)
		{
			PropertyHasChanged(propertyInfo);
		}
	}

	/// <summary>
	/// 比较非泛型入口的新旧值；业务对象按引用比较，其余按值比较。
	/// </summary>
	/// <param name="propertyInfo">属性。</param>
	/// <param name="newValue">新值。</param>
	/// <param name="oldValue">旧值。</param>
	/// <returns>不同则返回 <see langword="true"/>。</returns>
	private static bool ValuesDiffer(IPropertyInfo propertyInfo, object newValue, object oldValue)
	{
		if (oldValue == null)
		{
			return newValue != null;
		}

		if (typeof(IOsbaObject).IsAssignableFrom(propertyInfo.Type))
		{
			return !ReferenceEquals(oldValue, newValue);
		}

		return !Equals(oldValue, newValue);
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
}
