using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using Nerosoft.Euonia.Reflection;

// ReSharper disable MemberCanBeProtected.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Provides a base class for business objects, supporting property change notification, rule validation, and business
/// context management.
/// </summary>
/// <remarks>
/// BusinessObject implements interfaces for property change notification (INotifyPropertyChanged,
/// INotifyPropertyChanging), rule checking (IHasRuleCheck), and resource management (IDisposable). Derived classes
/// should override relevant methods to implement custom business logic and validation rules. The class manages rule
/// checking, tracks changed properties, and provides mechanisms to bypass rule checks when necessary. Thread safety and
/// event handling are supported for property and validation changes.
/// </remarks>
public abstract class BusinessObject : IBusinessObject, IHasRuleCheck, IDisposable
{
	private readonly List<IPropertyInfo> _changedProperties = [];

	/// <summary>
	/// The events manager for business object.
	/// </summary>
	protected readonly WeakEventManager Events = new();

	/// <summary>
	/// Gets or sets the business context.
	/// </summary>
	public BusinessContext BusinessContext
	{
		get;
		set
		{
			field = value;
			OnBusinessContextSet();
			Initialize();
			InitializeRules();
		}	
	}

	/// <summary>
	/// Handles the event when the BusinessContext is set.
	/// </summary>
	protected virtual void OnBusinessContextSet()
	{
	}

	/// <summary>
	/// Initializes the business object.
	/// </summary>
	protected virtual void Initialize()
	{
	}

	/// <summary>
	/// Occurs when property rule checks completed.
	/// </summary>
	public event EventHandler ValidationComplete
	{
		add => Events.AddEventHandler(value);
		remove => Events.RemoveEventHandler(value);
	}

	#region IHasRuleCheck implements

	/// <summary>
	/// To be added.
	/// </summary>
	/// <param name="property"></param>
	public void RuleCheckComplete(IPropertyInfo property)
	{
		OnPropertyChanged(property);
	}

	/// <summary>
	/// To be added.
	/// </summary>
	/// <param name="property"></param>
	public void RuleCheckComplete(string property)
	{
		OnPropertyChanged(property);
	}

	/// <summary>
	/// Complete all business object rules
	/// </summary>
	public void AllRulesComplete()
	{
		OnValidationComplete();
	}

	/// <summary>
	/// Suspends all rule checking, to be resumed later.
	/// </summary>
	public void SuspendRuleChecking()
	{
		Rules.SuppressRuleChecking = true;
	}

	/// <summary>
	/// Resumes rule checking.
	/// </summary>
	public void ResumeRuleChecking()
	{
		Rules.SuppressRuleChecking = false;
	}

	/// <summary>
	/// Returns a collection of broken rules for this object instance.
	/// </summary>
	/// <returns>Collection of broken rules.</returns>
	public BrokenRuleCollection GetBrokenRules()
	{
		return Rules.BrokenRules;
	}

	#endregion

	#region Rule check

	/// <inheritdoc/>
	public virtual bool IsValid => Rules.IsValid;

	/// <summary>
	/// Gets the rules object for this business object.
	/// </summary>
	protected Rules Rules
	{
		get
		{
			if (field == null)
			{
				field = new Rules(this);
			}
			else if (field.Target == null)
			{
				field.SetTarget(this);
			}

			return field;
		}
	}

	/// <summary>
	/// Called when validation has completed
	/// </summary>
	/// <remarks>
	/// The ValidationComplete event will be raised up.
	/// </remarks>
	protected virtual void OnValidationComplete()
	{
		Events.HandleEvent(this, EventArgs.Empty, nameof(ValidationComplete));
	}

	/// <summary>
	/// Initializes the validation rules for the current type, ensuring that all required rules are set up before use.
	/// </summary>
	/// <remarks>This method is thread-safe and prevents concurrent initialization of rules for the same type. If an
	/// error occurs during initialization, any partially initialized rules are cleaned up to maintain consistency. Call
	/// this method before performing operations that depend on the type's validation rules.</remarks>
	private void InitializeRules()
	{
		var rules = RuleManager.GetRules(GetType());
		if (rules.Initialized)
		{
			return;
		}

		lock (rules)
		{
			if (rules.Initialized)
			{
				return;
			}

			try
			{
				Rules.AddDataAnnotations();
				AddRules();
				rules.Initialized = true;
			}
			catch (Exception)
			{
				RuleManager.CleanRules(GetType());
				throw;
			}
		}
	}

	/// <summary>
	/// Gets the registered property check rules for the business object.
	/// </summary>
	/// <returns></returns>
	protected RuleManager GetRegisteredRules()
	{
		return Rules.RuleManager;
	}

	/// <summary>
	/// Adds validation rules to the current context. Derived classes should override this method to specify custom
	/// validation logic.
	/// </summary>
	/// <remarks>
	/// Implementations should ensure that all necessary rules are added to maintain data integrity. This
	/// method is called during the initialization phase of the validation process.
	/// </remarks>
	protected virtual void AddRules()
	{
	}

	/// <summary>
	///  Checks the rules for the specified property and raises the OnPropertyChanged event for each property that has a rule violation.
	/// </summary>
	/// <param name="property"></param>
	protected virtual void CheckPropertyRules(IPropertyInfo property)
	{
		var propertyNames = Rules.CheckRules(property);
		foreach (var name in propertyNames)
		{
			OnPropertyChanged(name);
		}
	}

	#endregion

	#region INotifyPropertyChanged/INotifyPropertyChanging

	/// <summary>
	/// Gets a value indicate if check rule will call on property changed.
	/// </summary>
	protected internal bool CheckRuleOnPropertyChanged { get; } = false;

	/// <inheritdoc/>
	public event PropertyChangedEventHandler PropertyChanged;

	/// <inheritdoc/>
	public event PropertyChangingEventHandler PropertyChanging;

	/// <summary>
	/// Notifies that a property value has been changed.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	protected virtual void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Notifies that a property value has been changed.
	/// </summary>
	/// <param name="propertyInfo">The property that changed.</param>
	protected virtual void OnPropertyChanged(IPropertyInfo propertyInfo)
	{
		OnPropertyChanged(propertyInfo.Name);
	}

	/// <summary>
	/// Notifies that a property value is about to change.
	/// </summary>
	/// <param name="propertyName">The name of the property that is about to change.</param>
	protected virtual void OnPropertyChanging(string propertyName)
	{
		PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
	}

	/// <summary>
	/// Notifies that a property value is about to change.
	/// </summary>
	/// <param name="propertyInfo">The property that is about to change.</param>
	protected virtual void OnPropertyChanging(IPropertyInfo propertyInfo)
	{
		OnPropertyChanging(propertyInfo.Name);
	}

	/// <summary>
	/// Raises the PropertyChanged event for the specified property and value.
	/// </summary>
	/// <param name="name">The name of the property that changed.</param>
	/// <param name="value">The new value of the property.</param>
	protected virtual void OnPropertyChanged(string name, object value)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// Marks the specified property as being dirty, or changed.
	/// </summary>
	/// <param name="property"></param>
	protected virtual void PropertyHasChanged(IPropertyInfo property)
	{
		_changedProperties.Add(property);
		if (CheckRuleOnPropertyChanged)
		{
			CheckPropertyRules(property);
		}
		else
		{
			OnPropertyChanged(property);
		}
	}

	/// <summary>
	/// Marks the specified property as being dirty, or changed.
	/// </summary>
	/// <param name="propertyName"></param>
	protected void PropertyHasChanged(string propertyName)
	{
		PropertyHasChanged(FieldManager.GetRegisteredProperty(propertyName));
	}

	/// <summary>
	/// Gets the list of changed properties.
	/// </summary>
	public virtual IReadOnlyList<IPropertyInfo> ChangedProperties => _changedProperties;

	/// <summary>
	/// Checks if the object has changed properties.
	/// </summary>
	public virtual bool HasChangedProperties => ChangedProperties.Count > 0;

	#endregion

	#region Property Checks

	/// <summary>
	/// Gets or sets a value indicating whether the object should bypass property checks.
	/// </summary>
	protected virtual bool IsBypassingRuleChecks { get; set; }

	private BypassRuleChecksObject InternalBypassRuleChecks { get; set; }

	/// <summary>
	/// By wrapping this property inside Using block
	/// you can set property values on current business object
	/// without raising PropertyChanged events
	/// and checking user rights.
	/// </summary>
	protected internal BypassRuleChecksObject BypassRuleChecks => BypassRuleChecksObject.GetManager(this);

	/// <summary>
	/// Used to create an object that bypasses rule checks, allowing certain values to be set even if they are not strictly valid. 
	/// The object also allows developers to check whether certain rules are being bypassed at any given time.
	/// </summary>
	protected internal sealed class BypassRuleChecksObject : IDisposable
	{
		private BusinessObject _target;
		private static readonly Lock _lock = new();

		private BypassRuleChecksObject(BusinessObject target)
		{
			_target = target;
			_target.IsBypassingRuleChecks = true;
		}

		#region IDisposable Members

		/// <summary>
		/// Disposes the object.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes the object.
		/// </summary>
		/// <param name="dispose">Dispose flag.</param>
		private void Dispose(bool dispose)
		{
			DeRef();
		}

		/// <summary>
		/// Gets the BypassPropertyChecks object.
		/// </summary>
		/// <param name="target">The business object.</param>
		/// <returns></returns>
		public static BypassRuleChecksObject GetManager(BusinessObject target)
		{
			lock (_lock)
			{
				target.InternalBypassRuleChecks ??= new BypassRuleChecksObject(target);

				target.InternalBypassRuleChecks.AddRef();
			}

			return target.InternalBypassRuleChecks;
		}

		#region Reference counting

		private int _refCount;

		/// <summary>
		/// Gets the current reference count for this
		/// object.
		/// </summary>
		public int RefCount => _refCount;

		private void AddRef()
		{
			_refCount += 1;
		}

		private void DeRef()
		{
			lock (_lock)
			{
				_refCount -= 1;
				if (_refCount != 0)
				{
					return;
				}

				_target.IsBypassingRuleChecks = false;
				_target.InternalBypassRuleChecks = null;
				_target = null;
			}
		}

		#endregion

		#endregion
	}

	#endregion

	/// <summary>
	/// Registers a property on the business object.
	/// </summary>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="objectType"></param>
	/// <param name="info"></param>
	/// <returns></returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(Type objectType, PropertyInfo<TValue> info)
	{
		return PropertyInfoManager.RegisterProperty(objectType, info);
	}

	#region Fields

	/// <inheritdoc/>
	public FieldDataManager FieldManager => field ??= new FieldDataManager(GetType());

	#endregion

	#region Read Properties

	/// <summary>
	/// Gets a property's value from the list of managed field values, converting the value to an appropriate type.
	/// </summary>
	/// <param name="propertyInfo">PropertyInfo object containing property metadata.</param>
	/// <typeparam name="TValue">Type of the field.</typeparam>
	/// <typeparam name="TProperty">Type of the property.</typeparam>
	/// <returns></returns>
	protected TProperty ReadPropertyConvert<TValue, TProperty>(PropertyInfo<TValue> propertyInfo)
	{
		return TypeHelper.CoerceValue<TProperty>(typeof(TValue), ReadProperty(propertyInfo));
	}

	/// <inheritdoc />
	public TValue ReadProperty<TValue>(PropertyInfo<TValue> propertyInfo)
	{
		TValue result;
		var data = FieldManager.GetFieldData(propertyInfo);
		if (data != null)
		{
			if (data is IFieldData<TValue> fd)
				result = fd.Value;
			else
				result = (TValue)data.Value;
		}
		else
		{
			result = propertyInfo.DefaultValue;
			FieldManager.LoadFieldData(propertyInfo, result);
		}

		return result;
	}

	/// <summary>
	/// Gets a property's value.
	/// </summary>
	/// <param name="propertyInfo">PropertyInfo object containing property metadata.</param>
	/// <returns></returns>
	public virtual object ReadProperty(IPropertyInfo propertyInfo)
	{
		object result;
		var info = FieldManager.GetFieldData(propertyInfo);
		if (info != null)
		{
			result = info.Value;
		}
		else
		{
			result = propertyInfo.DefaultValue;
			FieldManager.LoadFieldData(propertyInfo, result);
		}

		return result;
	}

	/// <summary>
	/// Gets a property's value by property name.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public virtual object ReadProperty(string propertyName)
	{
		var propertyInfo = FieldManager.GetRegisteredProperty(propertyName);
		if (propertyInfo == null)
		{
			throw new InvalidOperationException($"Property {propertyName} is not registered.");
		}

		return ReadProperty(propertyInfo);
	}

	/// <summary>
	/// Reads the value of the specified property by its name and returns it as the requested type.
	/// </summary>
	/// <param name="propertyName">The name of the property to read. Must represent a readable property.</param>
	/// <typeparam name="TValue">The type of the property value to be read.</typeparam>
	/// <returns>The value of the specified property, cast to the type specified by <typeparamref name="TValue"/>.</returns>
	/// <exception cref="InvalidOperationException">
	///	Thrown if the property name provided does not correspond to a valid property that can be read, or if the value cannot be cast to the specified type.
	/// </exception>
	public virtual TValue ReadProperty<TValue>(string propertyName)
	{
		var propertyInfo = FieldManager.GetRegisteredProperty(propertyName);
		
		if (propertyInfo == null)
		{
			throw new InvalidOperationException($"Property {propertyName} is not registered.");
		}

		if (propertyInfo is not PropertyInfo<TValue> property)
		{
			throw new InvalidOperationException("The property type does not match the expected type.");
		}

		{
		}

		return ReadProperty(property);
	}

	#endregion

	#region Load Properties

	/// <inheritdoc />
	public bool FieldExists(IPropertyInfo property)
	{
		return FieldManager.FieldExists(property);
	}

	/// <inheritdoc />
	public void LoadProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue)
	{
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

		LoadPropertyValue(propertyInfo, oldValue, newValue, false);
	}

	/// <summary>
	/// Loads a new value for the specified property and updates its state if the value has changed.
	/// </summary>
	/// <remarks>
	/// If the new value differs from the old value and <paramref name="markAsChanged"/> is <see
	/// langword="true"/>, the method triggers property change notifications and updates the property's state accordingly.
	/// Otherwise, the value is loaded without marking the property as changed.
	/// </remarks>
	/// <typeparam name="TValue">The type of the property's value.</typeparam>
	/// <param name="propertyInfo">The metadata that identifies the property whose value is being loaded.</param>
	/// <param name="oldValue">The previous value of the property before the update.</param>
	/// <param name="newValue">The new value to assign to the property.</param>
	/// <param name="markAsChanged">Indicates whether to mark the property as changed and trigger change notifications if the value has changed.</param>
	protected void LoadPropertyValue<TValue>(IPropertyInfo propertyInfo, TValue oldValue, TValue newValue, bool markAsChanged)
	{
		var valuesDiffer = ValuesDiffer(propertyInfo, newValue, oldValue);

		if (!valuesDiffer)
		{
			return;
		}

		if (markAsChanged)
		{
			OnPropertyChanging(propertyInfo);
			FieldManager.SetFieldData(propertyInfo, newValue);
			PropertyHasChanged(propertyInfo);
		}
		else
		{
			FieldManager.LoadFieldData(propertyInfo, newValue);
		}
	}

	/// <inheritdoc/>
	public virtual void LoadProperty(IPropertyInfo propertyInfo, object newValue)
	{
#if IOS
        //manually call LoadProperty<T> if the type is nullable otherwise JIT error will occur
        if (propertyInfo.Type == typeof(int?))
        {
            LoadProperty((PropertyInfo<int?>)propertyInfo, (int?)newValue);
        }
        else if (propertyInfo.Type == typeof(bool?))
        {
            LoadProperty((PropertyInfo<bool?>)propertyInfo, (bool?)newValue);
        }
        else if (propertyInfo.Type == typeof(DateTime?))
        {
            LoadProperty((PropertyInfo<DateTime?>)propertyInfo, (DateTime?)newValue);
        }
        else if (propertyInfo.Type == typeof(decimal?))
        {
            LoadProperty((PropertyInfo<decimal?>)propertyInfo, (decimal?)newValue);
        }
        else if (propertyInfo.Type == typeof(double?))
        {
            LoadProperty((PropertyInfo<double?>)propertyInfo, (double?)newValue);
        }
        else if (propertyInfo.Type == typeof(long?))
        {
            LoadProperty((PropertyInfo<long?>)propertyInfo, (long?)newValue);
        }
        else if (propertyInfo.Type == typeof(byte?))
        {
            LoadProperty((PropertyInfo<byte?>)propertyInfo, (byte?)newValue);
        }
        else if (propertyInfo.Type == typeof(char?))
        {
            LoadProperty((PropertyInfo<char?>)propertyInfo, (char?)newValue);
        }
        else if (propertyInfo.Type == typeof(short?))
        {
            LoadProperty((PropertyInfo<short?>)propertyInfo, (short?)newValue);
        }
        else if (propertyInfo.Type == typeof(uint?))
        {
            LoadProperty((PropertyInfo<uint?>)propertyInfo, (uint?)newValue);
        }
        else if (propertyInfo.Type == typeof(ulong?))
        {
            LoadProperty((PropertyInfo<ulong?>)propertyInfo, (ulong?)newValue);
        }
        else if (propertyInfo.Type == typeof(ushort?))
        {
            LoadProperty((PropertyInfo<ushort?>)propertyInfo, (ushort?)newValue);
        }
        else
        {
            LoadPropertyByReflection(nameof(LoadProperty), propertyInfo, newValue);
        }
#else
		_ = LoadPropertyByReflection(nameof(LoadProperty), propertyInfo, newValue);
#endif
	}

	/// <summary>
	/// Calls the generic LoadProperty method via reflection.
	/// </summary>
	/// <param name="methodName">The LoadProperty method name to call via reflection.</param>
	/// <param name="propertyInfo">PropertyInfo object containing property metadata.</param>
	/// <param name="newValue">The new value for the property.</param>
	/// <returns></returns>
	/// <exception cref="MissingMethodException"></exception>
	private object LoadPropertyByReflection(string methodName, IPropertyInfo propertyInfo, object newValue)
	{
		var type = GetType();
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		var method = type.GetMethods(flags).FirstOrDefault(c => c.Name == methodName && c.IsGenericMethod);
		if (method == null)
		{
			throw new MissingMethodException(type.FullName, methodName);
		}

		var genericMethod = method.MakeGenericMethod(propertyInfo.Type);
		var parameters = new[] { propertyInfo, newValue };
		return genericMethod.Invoke(this, parameters);
	}

	/// <summary>
	/// Determines whether the specified new and old values for a property are different.
	/// </summary>
	/// <remarks>
	/// For properties whose type implements IBusinessObject, this method uses reference equality to
	/// determine if the values differ. For other types, value equality is used. Null values are handled
	/// appropriately.
	/// </remarks>
	/// <typeparam name="TValue">The type of the values to compare.</typeparam>
	/// <param name="propertyInfo">The property metadata used to determine the comparison strategy based on the property's type.</param>
	/// <param name="newValue">The new value to compare. May be null.</param>
	/// <param name="oldValue">The old value to compare. May be null.</param>
	/// <returns>true if the new value differs from the old value; otherwise, false.</returns>
	protected virtual bool ValuesDiffer<TValue>(IPropertyInfo propertyInfo, TValue newValue, TValue oldValue)
	{
		bool valuesDiffer;
		if (oldValue == null)
		{
			valuesDiffer = newValue != null;
		}
		else
		{
			// use reference equals for objects that inherit from base class
			if (typeof(IBusinessObject).IsAssignableFrom(propertyInfo.Type))
			{
				valuesDiffer = !(ReferenceEquals(oldValue, newValue));
			}
			else
			{
				valuesDiffer = !EqualityComparer<TValue>.Default.Equals(newValue, oldValue);
			}
		}

		return valuesDiffer;
	}

	#endregion

	#region Authorization

	/// <summary>
	/// Determines whether the specified property can be read.
	/// </summary>
	/// <param name="property"></param>
	/// <returns></returns>
	public virtual bool CanReadProperty(IPropertyInfo property)
	{
		return true;
	}

	/// <summary>
	/// Determines whether the specified property can be read.
	/// </summary>
	/// <param name="property"></param>
	/// <param name="throwOnFalse"></param>
	/// <returns></returns>
	/// <exception cref="SecurityException"></exception>
	public bool CanReadProperty(IPropertyInfo property, bool throwOnFalse)
	{
		var result = CanReadProperty(property);
		if (throwOnFalse && !result)
		{
			throw new SecurityException($"Property get not allowed. {property.Name}");
		}

		return result;
	}

	/// <summary>
	/// Determines whether the specified property can be read.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	public bool CanReadProperty(string propertyName)
	{
		return CanReadProperty(propertyName, false);
	}

	private bool CanReadProperty(string propertyName, bool throwOnFalse)
	{
		var propertyInfo = FieldManager.GetRegisteredProperties().FirstOrDefault(p => p.Name == propertyName);
		if (propertyInfo == null)
		{
			Trace.TraceError("CanReadProperty: {0} is not a registered property of {1}.{2}", propertyName, this.GetType().Namespace, this.GetType().Name);
			return true;
		}

		{
		}
		return CanReadProperty(propertyInfo, throwOnFalse);
	}

	/// <summary>
	/// Determines whether the specified property can be set.
	/// </summary>
	/// <param name="property"></param>
	/// <returns></returns>
	public virtual bool CanWriteProperty(IPropertyInfo property)
	{
		return true;
	}

	/// <summary>
	/// Determines whether the specified property can be set.
	/// </summary>
	/// <param name="property"></param>
	/// <param name="throwOnFalse"></param>
	/// <returns></returns>
	/// <exception cref="SecurityException"></exception>
	public bool CanWriteProperty(IPropertyInfo property, bool throwOnFalse)
	{
		var result = CanWriteProperty(property);
		if (throwOnFalse && result == false)
		{
			throw new SecurityException($"Property set not allowed. {property.Name}");
		}

		return result;
	}

	/// <summary>
	/// Determines whether the specified property can be set.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	public bool CanWriteProperty(string propertyName)
	{
		return CanWriteProperty(propertyName, false);
	}

	/// <summary>
	/// Returns true if the user is allowed to write the specified property.
	/// </summary>
	/// <param name="propertyName">Name of the property to write.</param>
	/// <param name="throwOnFalse">Indicates whether a negative result should cause an exception.</param>
	/// <returns><c>True</c> if the user is allowed to write property value, otherwise <c>False</c></returns>
	private bool CanWriteProperty(string propertyName, bool throwOnFalse)
	{
		var propertyInfo = FieldManager.GetRegisteredProperties().FirstOrDefault(p => p.Name == propertyName);
		if (propertyInfo == null)
		{
			Trace.TraceError("CanReadProperty: {0} is not a registered property of {1}.{2}", propertyName, this.GetType().Namespace, this.GetType().Name);
			return true;
		}

		return CanWriteProperty(propertyInfo, throwOnFalse);
	}

	#endregion

	#region IDisposable

	private bool _disposedValue;

	/// <summary>
	/// Disposable pattern implementation.
	/// </summary>
	/// <param name="disposing"></param>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue)
		{
			return;
		}

		if (disposing)
		{
			// 释放托管状态(托管对象)
		}

		// 释放未托管的资源(未托管的对象)并重写终结器
		// 将大型字段设置为 null
		_disposedValue = true;
	}

	// Only override finalizer if 'Dispose(bool disposing)' has code to free unmanaged resources
	/// <summary>
	/// 
	/// </summary>
	~BusinessObject()
	{
		// 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
		Dispose(disposing: false);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		// 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}