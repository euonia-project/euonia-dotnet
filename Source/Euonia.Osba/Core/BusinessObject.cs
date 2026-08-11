using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using Nerosoft.Euonia.Reflection;

// ReSharper disable MemberCanBeProtected.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 为业务对象提供基类，支持属性更改通知、规则验证和业务上下文管理。
/// </summary>
/// <remarks>
/// BusinessObject 实现了属性更改通知（INotifyPropertyChanged、INotifyPropertyChanging）、
/// 规则检查（IHasRuleCheck）和资源管理（IDisposable）的接口。派生类应重写相关方法以
/// 实现自定义业务逻辑和验证规则。该类管理规则检查、跟踪已更改的属性，并在必要时提供绕过
/// 规则检查的机制。支持属性和验证更改的线程安全与事件处理。
/// </remarks>
public abstract class BusinessObject : IBusinessObject, IHasRuleCheck, IDisposable
{
	/// <summary>
	/// 已更改属性的列表。
	/// </summary>
	private readonly List<IPropertyInfo> _changedProperties = [];

	/// <summary>
	/// 保护 <see cref="_changedProperties"/> 并发访问的同步锁。
	/// </summary>
	private readonly Lock _changedPropertiesLock = new();

	/// <summary>
	/// 业务对象的事件管理器。
	/// </summary>
	protected readonly WeakEventManager Events = new();

	/// <summary>
	/// 获取或设置业务上下文。
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
	/// 当设置 BusinessContext 时处理该事件。
	/// </summary>
	protected virtual void OnBusinessContextSet()
	{
	}

	/// <summary>
	/// 初始化业务对象。
	/// </summary>
	protected virtual void Initialize()
	{
	}

	/// <summary>
	/// 当属性规则检查完成时发生。
	/// </summary>
	public event EventHandler ValidationComplete
	{
		add => Events.AddEventHandler(value);
		remove => Events.RemoveEventHandler(value);
	}

	#region IHasRuleCheck implements

	/// <summary>
	/// 指示某个属性规则检查已完成。
	/// </summary>
	/// <param name="property">规则所针对的属性信息。</param>
	public void RuleCheckComplete(IPropertyInfo property)
	{
		OnPropertyChanged(property);
	}

	/// <summary>
	/// 指示某个属性规则检查已完成。
	/// </summary>
	/// <param name="property">规则所针对的属性名称。</param>
	public void RuleCheckComplete(string property)
	{
		OnPropertyChanged(property);
	}

	/// <summary>
	/// 完成所有业务对象规则。
	/// </summary>
	public void AllRulesComplete()
	{
		OnValidationComplete();
	}

	/// <summary>
	/// 恰挂起所有规则检查，稍后可恢复。
	/// </summary>
	public void SuspendRuleChecking()
	{
		Rules.SuppressRuleChecking = true;
	}

	/// <summary>
	/// 恢复规则检查。
	/// </summary>
	public void ResumeRuleChecking()
	{
		Rules.SuppressRuleChecking = false;
	}

	/// <summary>
	/// 返回此对象实例的违规规则集合。
	/// </summary>
	/// <returns>违规规则集合。</returns>
	public BrokenRuleCollection GetBrokenRules()
	{
		return Rules.BrokenRules;
	}

	#endregion

	#region Rule check

	/// <inheritdoc/>
	public virtual bool IsValid => Rules.IsValid;

	/// <summary>
	/// 获取此业务对象的规则对象。
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
	/// 当验证完成时调用。
	/// </summary>
	/// <remarks>
	/// 将引发 ValidationComplete 事件。
	/// </remarks>
	protected virtual void OnValidationComplete()
	{
		Events.HandleEvent(this, EventArgs.Empty, nameof(ValidationComplete));
	}

	/// <summary>
	/// 初始化当前类型的验证规则，确保在使用前设置好所有必需的规则。
	/// </summary>
	/// <remarks>此方法线程安全，可防止同一类型的规则被并发初始化。如果初始化期间发生错误，
	/// 任何部分初始化的规则都会被清理以保持一致性。在依赖类型验证规则的操作之前调用此方法。</remarks>
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
	/// 获取业务对象的已注册属性检查规则。
	/// </summary>
	/// <returns>规则管理器。</returns>
	protected RuleManager GetRegisteredRules()
	{
		return Rules.RuleManager;
	}

	/// <summary>
	/// 向当前上下文添加验证规则。派生类应重写此方法以指定自定义验证逻辑。
	/// </summary>
	/// <remarks>
	/// 实现应确保添加所有必要的规则以保持数据完整性。此方法在验证过程的初始化阶段被调用。
	/// </remarks>
	protected virtual void AddRules()
	{
	}

	/// <summary>
	/// 检查指定属性的规则，并为每个存在规则违规的属性引发 OnPropertyChanged 事件。
	/// </summary>
	/// <param name="property">要检查规则的属性信息。</param>
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
	/// 获取一个值，指示检查规则是否将调用属性更改。
	/// </summary>
	protected internal bool CheckRuleOnPropertyChanged { get; } = false;

	/// <inheritdoc/>
	public event PropertyChangedEventHandler PropertyChanged;

	/// <inheritdoc/>
	public event PropertyChangingEventHandler PropertyChanging;

	/// <summary>
	/// 通知属性值已更改。
	/// </summary>
	/// <param name="propertyName">已更改属性的名称。</param>
	protected virtual void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// 通知属性值已更改。
	/// </summary>
	/// <param name="propertyInfo">已更改的属性。</param>
	protected virtual void OnPropertyChanged(IPropertyInfo propertyInfo)
	{
		OnPropertyChanged(propertyInfo.Name);
	}

	/// <summary>
	/// 通知属性值即将更改。
	/// </summary>
	/// <param name="propertyName">即将更改的属性的名称。</param>
	protected virtual void OnPropertyChanging(string propertyName)
	{
		PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
	}

	/// <summary>
	/// 通知属性值即将更改。
	/// </summary>
	/// <param name="propertyInfo">即将更改的属性。</param>
	protected virtual void OnPropertyChanging(IPropertyInfo propertyInfo)
	{
		OnPropertyChanging(propertyInfo.Name);
	}

	/// <summary>
	/// 为指定属性和值引发 PropertyChanged 事件。
	/// </summary>
	/// <param name="name">已更改属性的名称。</param>
	/// <param name="value">属性的新值。</param>
	protected virtual void OnPropertyChanged(string name, object value)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// 将指定属性标记为脏的或已更改。
	/// </summary>
	/// <param name="property">要标记的属性信息。</param>
	protected virtual void PropertyHasChanged(IPropertyInfo property)
	{
		lock (_changedPropertiesLock)
		{
			if (!_changedProperties.Contains(property))
			{
				_changedProperties.Add(property);
			}
		}

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
	/// 将指定属性标记为脏的或已更改。
	/// </summary>
	/// <param name="propertyName">要标记的属性名称。</param>
	protected void PropertyHasChanged(string propertyName)
	{
		PropertyHasChanged(FieldManager.GetRegisteredProperty(propertyName));
	}

	/// <summary>
	/// 获取已更改属性的列表快照，可安全跨线程读取。
	/// </summary>
	public virtual IReadOnlyList<IPropertyInfo> ChangedProperties
	{
		get
		{
			lock (_changedPropertiesLock)
			{
				return [.. _changedProperties];
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

	#endregion

	#region Property Checks

	/// <summary>
	/// 获取或设置一个值，指示对象是否应绕过属性检查。
	/// </summary>
	protected virtual bool IsBypassingRuleChecks { get; set; }

	private BypassRuleChecksObject InternalBypassRuleChecks { get; set; }

	/// <summary>
	/// 通过将此属性包裹在 Using 块中，可以在不引发 PropertyChanged 事件
	/// 和不检查用户权限的情况下，为当前业务对象设置属性值。
	/// </summary>
	protected internal BypassRuleChecksObject BypassRuleChecks => BypassRuleChecksObject.GetManager(this);

	/// <summary>
	/// 用于创建绕过规则检查的对象，允许设置某些即使不是严格有效的值。
	/// 该对象还允许开发者在任何时候检查某些规则是否正在被绕过。
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
		/// 释放对象。
		/// </summary>
		public void Dispose()
		{
			DeRef();
		}

		/// <summary>
		/// 获取 BypassPropertyChecks 对象。
		/// </summary>
		/// <param name="target">业务对象。</param>
		/// <returns>绕过规则检查的管理器对象。</returns>
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
		/// 获取此对象的当前引用计数。
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
				if (_refCount == 0)
				{
					// 已经释放，防止重复释放导致引用计数为负或空引用
					return;
				}

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
	/// 在业务对象上注册一个属性。
	/// </summary>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <param name="objectType">属性所属的对象类型。</param>
	/// <param name="info">属性信息。</param>
	/// <returns>注册的属性信息。</returns>
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
	/// 从托管字段值列表中获取属性值，并将值转换为适当的类型。
	/// </summary>
	/// <param name="propertyInfo">包含属性元数据的 PropertyInfo 对象。</param>
	/// <typeparam name="TValue">字段的类型。</typeparam>
	/// <typeparam name="TProperty">属性的类型。</typeparam>
	/// <returns>转换后的属性值。</returns>
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
	/// 获取属性值。
	/// </summary>
	/// <param name="propertyInfo">包含属性元数据的 PropertyInfo 对象。</param>
	/// <returns>属性的值。</returns>
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
	/// 按属性名称获取属性值。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>属性的值。</returns>
	/// <exception cref="InvalidOperationException">当属性未注册时抛出。</exception>
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
	/// 按名称读取指定属性的值，并将其作为请求的类型返回。
	/// </summary>
	/// <param name="propertyName">要读取的属性名称。必须表示可读属性。</param>
	/// <typeparam name="TValue">要读取的属性值的类型。</typeparam>
	/// <returns>指定属性的值，转换为 <typeparamref name="TValue"/> 指定的类型。</returns>
	/// <exception cref="InvalidOperationException">
	///	当提供的属性名称不对应于可读的有效属性，或值无法转换为指定类型时抛出。
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
			throw new InvalidOperationException($"Property '{propertyName}' is registered as '{propertyInfo.Type.Name}', which does not match the expected type '{typeof(TValue).Name}'.");
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
				_ = FieldManager.LoadFieldData(propertyInfo, oldValue);
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
	/// 为指定属性加载新值，并在值已更改时更新其状态。
	/// </summary>
	/// <remarks>
	/// 如果新值不同于旧值且 <paramref name="markAsChanged"/> 为 <see
	/// langword="true"/>，此方法会触发属性更改通知并相应地更新属性状态。
	/// 否则，加载值但不将属性标记为已更改。
	/// </remarks>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <param name="propertyInfo">标识要加载值的属性的元数据。</param>
	/// <param name="oldValue">更新前属性的先前值。</param>
	/// <param name="newValue">要赋给属性的新值。</param>
	/// <param name="markAsChanged">指示是否将属性标记为已更改并在值已更改时触发更改通知。</param>
	/// <param name="onChanged">值更改时的回调操作。</param>
	protected void LoadPropertyValue<TValue>(IPropertyInfo propertyInfo, TValue oldValue, TValue newValue, bool markAsChanged, Action<IPropertyInfo, TValue, TValue> onChanged = null)
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
			onChanged?.Invoke(propertyInfo, oldValue, newValue);
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
        // 如果类型为可空类型，则手动调用 LoadProperty<T>，否则将发生 JIT 错误
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
	/// 缓存已构造的泛型 <see cref="LoadProperty{TValue}"/> 方法，避免重复反射查找。
	/// </summary>
	private static readonly ConcurrentDictionary<(Type DeclaringType, Type PropertyType), MethodInfo> _loadPropertyMethodCache = new();

	/// <summary>
	/// 通过反射调用泛型 LoadProperty 方法。
	/// </summary>
	/// <param name="methodName">要通过反射调用的 LoadProperty 方法名。</param>
	/// <param name="propertyInfo">包含属性元数据的 PropertyInfo 对象。</param>
	/// <param name="newValue">属性的新值。</param>
	/// <returns>反射调用的返回值。</returns>
	/// <exception cref="MissingMethodException">当找不到指定的泛型方法时抛出。</exception>
	private object LoadPropertyByReflection(string methodName, IPropertyInfo propertyInfo, object newValue)
	{
		var type = GetType();
		var genericMethod = _loadPropertyMethodCache.GetOrAdd((type, propertyInfo.Type), _ =>
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var method = type.GetMethods(flags).FirstOrDefault(c => c.Name == methodName && c.IsGenericMethod);
			if (method == null)
			{
				throw new MissingMethodException(type.FullName, methodName);
			}

			return method.MakeGenericMethod(propertyInfo.Type);
		});

		var parameters = new[] { propertyInfo, newValue };
		return genericMethod.Invoke(this, parameters);
	}

	/// <summary>
	/// 确定属性的指定新旧值是否不同。
	/// </summary>
	/// <remarks>
	/// 对于类型实现 IBusinessObject 的属性，此方法使用引用相等性来确定值是否不同。
	/// 对于其他类型，使用值相等性。null 值会被适当处理。
	/// </remarks>
	/// <typeparam name="TValue">要比较的值的类型。</typeparam>
	/// <param name="propertyInfo">用于根据属性类型确定比较策略的属性元数据。</param>
	/// <param name="newValue">要比较的新值。可以为 <c>null</c>。</param>
	/// <param name="oldValue">要比较的旧值。可以为 <c>null</c>。</param>
	/// <returns>如果新值不同于旧值，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	protected virtual bool ValuesDiffer<TValue>(IPropertyInfo propertyInfo, TValue newValue, TValue oldValue)
	{
		bool valuesDiffer;
		if (oldValue == null)
		{
			valuesDiffer = newValue != null;
		}
		else
		{
			// 对继承自基类的对象使用引用相等比较
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
	/// 确定是否可以读取指定属性。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>如果可以读取，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public virtual bool CanReadProperty(IPropertyInfo property)
	{
		return true;
	}

	/// <summary>
	/// 确定是否可以读取指定属性。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <param name="throwOnFalse">指示否定结果是否应导致异常。</param>
	/// <returns>如果可以读取，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	/// <exception cref="SecurityException">当不允许读取且 <paramref name="throwOnFalse"/> 为 <c>true</c> 时抛出。</exception>
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
	/// 确定是否可以读取指定属性。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>如果可以读取，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool CanReadProperty(string propertyName)
	{
		return CanReadProperty(propertyName, false);
	}

	private bool CanReadProperty(string propertyName, bool throwOnFalse)
	{
		var propertyInfo = FieldManager.FindRegisteredProperty(propertyName);
		if (propertyInfo == null)
		{
			Trace.TraceError("CanReadProperty: {0} is not a registered property of {1}.{2}", propertyName, this.GetType().Namespace, this.GetType().Name);
			return true;
		}

		return CanReadProperty(propertyInfo, throwOnFalse);
	}

	/// <summary>
	/// 确定是否可以设置指定属性。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <returns>如果可以设置，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public virtual bool CanWriteProperty(IPropertyInfo property)
	{
		return true;
	}

	/// <summary>
	/// 确定是否可以设置指定属性。
	/// </summary>
	/// <param name="property">属性信息。</param>
	/// <param name="throwOnFalse">指示否定结果是否应导致异常。</param>
	/// <returns>如果可以设置，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	/// <exception cref="SecurityException">当不允许设置且 <paramref name="throwOnFalse"/> 为 <c>true</c> 时抛出。</exception>
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
	/// 确定是否可以设置指定属性。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>如果可以设置，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool CanWriteProperty(string propertyName)
	{
		return CanWriteProperty(propertyName, false);
	}

	/// <summary>
	/// 如果允许用户写入指定属性，则返回 <c>true</c>。
	/// </summary>
	/// <param name="propertyName">要写入的属性名称。</param>
	/// <param name="throwOnFalse">指示否定结果是否应导致异常。</param>
	/// <returns>如果允许用户写入属性值，则为 <c>True</c>；否则为 <c>False</c>。</returns>
	private bool CanWriteProperty(string propertyName, bool throwOnFalse)
	{
		var propertyInfo = FieldManager.FindRegisteredProperty(propertyName);
		if (propertyInfo == null)
		{
			Trace.TraceError("CanWriteProperty: {0} is not a registered property of {1}.{2}", propertyName, this.GetType().Namespace, this.GetType().Name);
			return true;
		}

		return CanWriteProperty(propertyInfo, throwOnFalse);
	}

	#endregion

	#region IDisposable

	private bool _disposedValue;

	/// <summary>
	/// 可释放模式的实现。
	/// </summary>
	/// <param name="disposing">指示是否正在释放托管资源。</param>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue)
		{
			return;
		}

		// 当前无托管/非托管资源需要释放，保留该重写方法供派生类扩展
		_disposedValue = true;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(disposing: true);
	}

	#endregion
}