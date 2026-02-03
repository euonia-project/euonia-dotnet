using Nerosoft.Euonia.Reflection;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents a business object that can be edited and saved.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class EditableObject<T> : BusinessObject<T>, IOperableProperty, IEditableObject, ISavable, ISavable<T>
	where T : EditableObject<T>
{
	/// <summary>
	/// Gets the current object state.
	/// </summary>
	public ObjectEditState State { get; private set; } = ObjectEditState.None;

	/// <summary>
	/// Gets a value indicating whether the object is new.
	/// </summary>
	public bool IsNew => State == ObjectEditState.New;

	/// <summary>
	/// Gets a value indicating whether the object has changed.
	/// </summary>
	public bool IsChanged => State == ObjectEditState.Changed;

	/// <summary>
	/// Gets a value indicating whether the object would be deleted.
	/// </summary>
	public bool IsDeleted => State == ObjectEditState.Deleted;

	/// <summary>
	/// Gets or sets a value indicating whether to check object rules on delete.
	/// </summary>
	public bool CheckObjectRulesOnDelete { get; private set; }

	/// <summary>
	/// Mark the object state as <see cref="ObjectEditState.New"/>.
	/// </summary>
	public void MarkAsNew()
	{
		State = ObjectEditState.New;
	}

	/// <summary>
	/// Mark the object state as <see cref="ObjectEditState.Changed"/>.
	/// </summary>
	public void MarkAsChanged()
	{
		State = ObjectEditState.Changed;
	}

	/// <summary>
	/// Mark the object state as <see cref="ObjectEditState.Deleted"/>.
	/// </summary>
	/// <param name="checkObjectRules"></param>
	public void MarkAsDeleted(bool checkObjectRules = false)
	{
		State = ObjectEditState.Deleted;
		CheckObjectRulesOnDelete = checkObjectRules;
	}

	/// <summary>
	/// Counter to track busy state.
	/// </summary>
	private int _isBusyCounter;

	/// <summary>
	/// Gets a value indicating whether the object is busy.
	/// </summary>
	public virtual bool IsBusy => IsSelfBusy || (FieldManager != null && FieldManager.IsBusy());

	/// <summary>
	/// Gets a value indicating whether the object itself is busy.
	/// </summary>
	public virtual bool IsSelfBusy => _isBusyCounter > 0 || Rules.HasRunningRules;

	private BusyChangedEventHandler _busyChanged;

	/// <summary>
	/// Event raised when the busy status changes.
	/// </summary>
	public event BusyChangedEventHandler BusyChanged
	{
		// add => _busyChanged += value;
		// remove => _busyChanged -= value;
		add => _busyChanged = (BusyChangedEventHandler)Delegate.Combine(_busyChanged, value);
		remove => _busyChanged = (BusyChangedEventHandler)Delegate.Remove(_busyChanged, value);
	}

	/// <summary>
	/// Raises the <see cref="BusyChanged"/> event.
	/// </summary>
	/// <param name="args"></param>
	protected virtual void OnBusyChanged(BusyChangedEventArgs args)
	{
		_busyChanged?.Invoke(this, args);
	}

	/// <summary>
	/// Marks the object as busy.
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
	/// Marks the object as idle.
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

	#region ISavable implments

	/// <summary>
	/// Event raised when the object has been saved.
	/// </summary>
	public event EventHandler<SavedEventArgs> Saved
	{
		add => Events.AddEventHandler(value);
		remove => Events.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets a value indicating whether the object is savable.
	/// </summary>
	public virtual bool IsSavable => IsValid && (HasChangedProperties || IsChanged) && !IsBusy;

	/// <summary>
	/// Called when the object has been saved.
	/// </summary>
	/// <param name="newObject"></param>
	/// <param name="error"></param>
	/// <param name="userState"></param>
	protected virtual void OnSaved(T newObject, Exception error, object userState)
	{
		var args = new SavedEventArgs(newObject, error, userState);
		Events.HandleEvent(this, args, nameof(Saved));
	}

	void ISavable<T>.SaveComplete(T newObject)
	{
		OnSaved(newObject, null, null);
	}

	void ISavable.SaveComplete(object newObject)
	{
		OnSaved((T)newObject, null, null);
	}

	/// <summary>
	/// Save the object.
	/// </summary>
	/// <param name="forceUpdate"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<T> SaveAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)
	{
		return await SaveAsync(forceUpdate, null, cancellationToken);
	}

	async Task<object> ISavable.SaveAsync(bool forceUpdate, CancellationToken cancellationToken)
	{
		return await SaveAsync(forceUpdate, cancellationToken);
	}

	/// <summary>
	/// Save the object.
	/// </summary>
	/// <param name="forceUpdate"></param>
	/// <param name="userState"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="ValidationException"></exception>
	protected virtual async Task<T> SaveAsync(bool forceUpdate, object userState, CancellationToken cancellationToken = default)
	{
		if (State == ObjectEditState.None)
		{
			if (forceUpdate)
			{
				MarkAsChanged();
			}
			else
			{
				return (T)this;
			}
		}

		if (!IsDeleted || CheckObjectRulesOnDelete)
		{
			await Rules.CheckObjectRulesAsync(true, cancellationToken);
			if (Rules.HasRunningRules)
			{
				var task = new TaskCompletionSource<bool>();
				ValidationComplete += OnValidationCompleted;
				await task.Task;

				ValidationComplete -= OnValidationCompleted;

				void OnValidationCompleted(object sender, EventArgs args)
				{
					task.SetResult(true);
				}
			}
		}

		if (!IsValid && (!IsDeleted || CheckObjectRulesOnDelete))
		{
			var errors = Rules.BrokenRules.Select(t => new ValidationResult(t.Property, t.Description));
			throw new ValidationException("Object not valid for save.", errors);
		}

		MarkAsBusy();
		var result = await BusinessContext.GetRequiredService<IObjectFactory>().SaveAsync((T)this, cancellationToken);
		result?.MarkAsIdle();
		MarkAsIdle();
		OnSaved(result, null, userState);
		return result;
	}

	/// <summary>
	/// Create new editable object.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	protected internal virtual Task CreateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Indicates that the object has been saved.
	/// </summary>
	/// <param name="cancellationToken"></param>
	protected internal virtual Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Update the object.
	/// </summary>
	/// <param name="cancellationToken"></param>
	protected internal virtual Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Delete the object.
	/// </summary>
	/// <param name="cancellationToken"></param>
	protected internal virtual Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	#endregion

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
	/// Gets a property's value, first checking authorization.
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
	/// Gets a property's value, first checking authorization.
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
	/// Gets a property's value, first checking authorization.
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
	/// Gets a property's value, first checking authorization.
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
	/// Gets a property's value, first checking authorization.
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

	/// <summary>
	/// Gets value of <see cref="IPropertyInfo"/> property.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <returns></returns>
	public object GetProperty(IPropertyInfo propertyInfo)
	{
		object result;
		if (IsBypassingRuleChecks || CanReadProperty(propertyInfo, false))
		{
			// call ReadProperty (maybe overloaded in actual class)
			result = ReadProperty(propertyInfo);
		}
		else
		{
			result = propertyInfo.DefaultValue;
		}

		return result;
	}

	/// <summary>
	/// Gets value of <see cref="IPropertyInfo"/> property.
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
	/// Sets a property's value, first checking authorization.
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
	/// Sets a property's value, first checking authorization.
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
	/// Sets a property's value, first checking authorization.
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

			if (ValuesDiffer(propertyInfo, newValue, field))
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

			field = newValue;
			if (!IsBypassingRuleChecks)
			{
				PropertyHasChanged(propertyName);
			}
		}
	}

	/// <summary>
	/// Sets a property's value, first checking authorization.
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
	/// Sets a property's value, first checking authorization.
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
	/// Sets a property's value, first checking authorization.
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

	/// <summary>
	/// Sets a property's value, first checking authorization.
	/// </summary>
	/// <param name="propertyInfo"></param>
	/// <param name="newValue"></param>
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
	/// Sets a property's value, first checking authorization.
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