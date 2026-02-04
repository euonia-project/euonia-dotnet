using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The read only object.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ReadOnlyObject<T> : BusinessObject<T>, IReadOnlyObject, IOperableProperty
	where T : ReadOnlyObject<T>
{
	/// <summary>
	/// Override the IsBypassingRuleChecks to prevent the PropertyChanged event raise up.
	/// </summary>
	protected override bool IsBypassingRuleChecks
	{
		get => true;
	}

	#region Get Properties

	/// <summary>
	/// Gets a property's value, first checking authorization.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <param name="field"></param>
	/// <param name="defaultValue"></param>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
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
	/// Gets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="field"></param>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
	protected TValue GetProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue field)
	{
		return GetProperty(propertyInfo.Name, field, propertyInfo.DefaultValue);
	}

	/// <summary>
	/// Gets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="field"></param>
	/// <param name="defaultValue"></param>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
	protected TValue GetProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue field, TValue defaultValue)
	{
		return GetProperty(propertyInfo.Name, field, defaultValue);
	}

	/// <summary>
	/// Gets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="field"></param>
	/// <typeparam name="TField"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
	protected TValue GetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo, TField field)
	{
		return TypeHelper.CoerceValue<TValue>(typeof(TField), GetProperty(propertyInfo.Name, field, propertyInfo.DefaultValue));
	}

	/// <summary>
	/// Gets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <typeparam name="TField"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
	protected TValue GetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo)
	{
		return TypeHelper.CoerceValue<TValue>(typeof(TField), GetProperty(propertyInfo));
	}

	/// <summary>
	/// Gets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
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
			// call ReadProperty (may be overloaded in actual class)
			result = ReadProperty(propertyInfo);
		}
		else
		{
			result = propertyInfo.DefaultValue;
		}

		return result;
	}

	/// <summary>
	/// Gets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
	protected TValue GetProperty<TValue>(IPropertyInfo propertyInfo)
	{
		return (TValue)GetProperty(propertyInfo);
	}

	#endregion

	#region Set Properties

	/// <summary>
	/// Sets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="field"></param>
	/// <param name="newValue"></param>
	/// <typeparam name="TValue"></typeparam>
	protected void SetProperty<TValue>(PropertyInfo<TValue> propertyInfo, ref TValue field, TValue newValue)
	{
		SetProperty(propertyInfo.Name, ref field, newValue);
	}

	/// <summary>
	/// Sets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="field"></param>
	/// <param name="newValue"></param>
	/// <typeparam name="TField"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	protected void SetPropertyConvert<TField, TValue>(PropertyInfo<TField> propertyInfo, ref TField field, TValue newValue)
	{
		SetPropertyConvert(propertyInfo.Name, ref field, newValue);
	}

	/// <summary>
	/// Sets the value of specified property.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <param name="field"></param>
	/// <param name="newValue"></param>
	/// <typeparam name="TValue"></typeparam>
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
	/// Sets the value of specified property.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <param name="field"></param>
	/// <param name="newValue"></param>
	/// <typeparam name="TField"></typeparam>
	/// <typeparam name="TValue"></typeparam>
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
	/// Sets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="newValue"></param>
	/// <typeparam name="TField"></typeparam>
	/// <typeparam name="TValue"></typeparam>
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
	/// Sets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="newValue"></param>
	/// <typeparam name="TValue"></typeparam>
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
	/// Sets the value of specified property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="newValue"></param>
	/// <typeparam name="TValue"></typeparam>
	protected virtual void SetProperty<TValue>(IPropertyInfo propertyInfo, TValue newValue)
	{
		SetProperty(propertyInfo, (object)newValue);
	}

	#endregion
}