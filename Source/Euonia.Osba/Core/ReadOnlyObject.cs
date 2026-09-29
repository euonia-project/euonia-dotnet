using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 只读对象。
/// </summary>
/// <typeparam name="T">只读对象的具体类型。</typeparam>
public class ReadOnlyObject<T> : BusinessObject<T>, IReadOnlyObject, IOperableProperty
	where T : ReadOnlyObject<T>
{
	/// <summary>
	/// 只读对象拒绝任何属性写入。
	/// </summary>
	/// <remarks>
	/// <para>此前这里靠重写 <c>IsBypassingRuleChecks</c> 来「让只读对象安静一点」，但那个开关的本意是
	/// 「本次绕过规则检查与权限检查」，被永久置真之后的副作用是：</para>
	/// <list type="bullet">
	/// <item>写入路径的 <c>!IsBypassingRuleChecks &amp;&amp; !CanWriteProperty(...)</c> 被整体短路——
	/// <strong>只读对象实际上可写</strong>，而且写完既不通知也不报错，是静默的数据损坏；</item>
	/// <item>读取路径的 <c>IsBypassingRuleChecks || CanReadProperty(...)</c> 同样被短路——权限对只读对象永不生效。</item>
	/// </list>
	/// <para>因此把「只读」这件事放回它该在的位置：覆盖 <see cref="BusinessObject.CanWriteProperty(IPropertyInfo)"/>。
	/// 写入由各 <c>SetProperty</c> 入口的 <c>CanWriteProperty(property, true)</c> 统一裁决
	/// （拒绝时抛 <see cref="System.Security.SecurityException"/>）；
	/// 显式的 <c>BypassRuleChecks</c> 仍然可以写（那是调用方主动声明的绕过，语义由 <c>ObservableObject</c> 统一定义）；
	/// 读取权限则回归 <c>CanReadProperty</c> 正常判定。</para>
	/// </remarks>
	/// <param name="property">待写入的属性。</param>
	/// <returns>恒为 <see langword="false"/>。</returns>
	public override bool CanWriteProperty(IPropertyInfo property)
	{
		return false;
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
	protected TValue GetProperty<TValue>(string propertyName, TValue field, TValue defaultValue)
	{
		#region Check to see if the property is marked with RelationshipTypes.PrivateField

		var propertyInfo = FieldManager.GetRegisteredProperty(propertyName);

		#endregion

		if (IsBypassingRuleChecks || CanReadProperty(propertyInfo, true))
		{
			return field;
		}

		return defaultValue;
	}

	/// <summary>
	/// 获取指定属性的值。
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
	/// 获取指定属性的值。
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
	/// 获取指定属性的值。
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
	/// 获取指定属性的值。
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
	/// 获取指定属性的值。
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

	/// <inheritdoc />
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
	/// 获取指定属性的值。
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
	/// 设置指定属性的值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="field">字段引用。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected void SetProperty<TValue>(PropertyInfo<TValue> propertyInfo, ref TValue field, TValue newValue)
	{
		SetProperty(propertyInfo.Name, ref field, newValue);
	}

	/// <summary>
	/// 设置指定属性的值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="field">字段引用。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TField">字段的类型。</typeparam>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected void SetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo, ref TField field, TValue newValue)
	{
		SetPropertyConvert(propertyInfo.Name, ref field, newValue);
	}

	/// <summary>
	/// 设置指定属性的值。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="field">字段引用。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected void SetProperty<TValue>(string propertyName, ref TValue field, TValue newValue)
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
				doChange = true;
		}
		else
		{
			if (typeof(TValue) == typeof(string) && newValue == null)
				newValue = TypeHelper.CoerceValue<TValue>(typeof(string), string.Empty);
			if (!field.Equals(newValue))
				doChange = true;
		}

		if (doChange)
		{
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
	}

	/// <summary>
	/// 设置指定属性的值。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="field">字段引用。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TField">字段的类型。</typeparam>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected void SetPropertyConvert<TField, TValue>(string propertyName, ref TField field, TValue newValue)
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
	/// 设置指定属性的值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TField">字段的类型。</typeparam>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected void SetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo, TValue newValue)
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

		LoadPropertyValue(propertyInfo, oldValue, TypeHelper.CoerceValue<TField>(typeof(TValue), newValue), !IsBypassingRuleChecks);
	}

	/// <summary>
	/// 设置指定属性的值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected void SetProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue)
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

		LoadPropertyValue(propertyInfo, oldValue, newValue, !IsBypassingRuleChecks);
	}

	/// <inheritdoc />
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
	/// 设置指定属性的值。
	/// </summary>
	/// <param name="propertyInfo">属性信息。</param>
	/// <param name="newValue">新值。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	protected virtual void SetProperty<TValue>(IPropertyInfo propertyInfo, TValue newValue)
	{
		SetProperty(propertyInfo, (object)newValue);
	}

	#endregion
}