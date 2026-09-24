using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using Nerosoft.Euonia.Reflection;

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
	/// 获取本对象的规则实例，供同程序集内的强制点（工厂边界、执行器）使用。
	/// </summary>
	/// <remarks>
	/// <see cref="Rules"/> 保持 <see langword="protected"/>，只开这一个 <see langword="internal"/> 口子，
	/// 不把规则集合变成公开 API。程序集外的调用方请用 <see cref="ValidateAsync"/>。
	/// </remarks>
	internal Rules RuleSet => Rules;

	/// <summary>
	/// 运行对象级规则检查，并返回对象是否有效。
	/// </summary>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>规则检查后 <see cref="IsValid"/> 的值。</returns>
	/// <remarks>
	/// <para>
	/// 本方法<b>不修改对象状态、也不抛异常</b>；失败明细经 <see cref="GetBrokenRules"/> 读取。
	/// 需要「不合规就抛」的语义请用 <see cref="EnsureValidAsync"/>，或直接保存（保存会自行检查）。
	/// </para>
	/// <para>
	/// 之所以需要显式的检查入口：<see cref="IsValid"/> 取自违规集合，而违规集合只有在
	/// <b>某次检查跑过之后</b>才有内容——首次检查之前它恒为 <see langword="true"/>。
	/// 因此 <c>IsSavable</c> 之类「读一下就知道能不能保存」的用法并不成立，
	/// 必须先经本方法（或保存）真正跑一遍规则。
	/// </para>
	/// <para>
	/// 规则检查被挂起（<see cref="SuspendRuleChecking"/>）时不会真正检查，
	/// 返回值就是<b>当前</b>的 <see cref="IsValid"/>（可能来自上一次检查），此时它不构成结论。
	/// </para>
	/// </remarks>
	public virtual async Task<bool> ValidateAsync(bool cascade = true, CancellationToken cancellationToken = default)
	{
		await Rules.CheckObjectRulesAsync(cascade, cancellationToken);
		return IsValid;
	}

	/// <summary>
	/// 运行对象级规则检查，存在 Error 级违规时抛出
	/// <see cref="Nerosoft.Euonia.Validation.ValidationException"/>。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// 与保存、命令执行共用同一套检查与异常形态（见 <see cref="ObjectRuleGuard"/>）。
	/// 规则检查被挂起（<see cref="SuspendRuleChecking"/>）时不给出结论、也不抛出。
	/// </remarks>
	/// <exception cref="Nerosoft.Euonia.Validation.ValidationException">存在 Error 级违规时抛出。</exception>
	public Task EnsureValidAsync(CancellationToken cancellationToken = default)
	{
		return Rules.EnsureObjectRulesAsync(cascade: true, "Object not valid.", cancellationToken);
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
				InjectScopePolicyRule(rules);
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
	/// 若本类型声明了数据权限模型，则自动注入 <see cref="ScopePolicyRule"/>。
	/// </summary>
	/// <param name="rules">本类型的规则管理器。</param>
	/// <remarks>
	/// <para>
	/// 注入使越权保存在保存前以验证错误暴露，无需使用方手工 <c>AddRule</c>，
	/// 从而消除「漏加规则 = 静默无保护」。
	/// </para>
	/// <para>
	/// <b>本方法绝不抛异常</b>：注册表缺失、环境态未建立、类型未声明模型等情况一律静默跳过。
	/// 规则是补充信号而非强制点，让它在属性 setter 上抛出会把配置问题伪装成难以定位的异常。
	/// </para>
	/// <para>
	/// 注入的规则是<b>无状态桥</b>，执行时才从业务上下文解析注册表与授权数据——
	/// 这是必需的，因为规则集合是进程级共享的，而注册表是按容器的。
	/// </para>
	/// </remarks>
	private void InjectScopePolicyRule(RuleManager rules)
	{
		try
		{
			var registry = BusinessContext?.GetService<ScopeModelRegistry>();

			if (registry == null || !registry.IsDeclared(GetType()))
			{
				return;
			}

			if (rules.Rules.OfType<ScopePolicyRule>().Any())
			{
				return;
			}

			Rules.AddRule(new ScopePolicyRule());
		}
		catch
		{
			// 环境态未建立时 GetService 会抛：静默跳过，规则不是强制点
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

	/// <summary>
	/// 提交当前更改：清除所有已更改属性的跟踪记录，并使字段的修改历史失效，
	/// 将字段当前值作为新的基线，令 <see cref="HasChangedProperties"/> 和字段的
	/// <see cref="FieldData{T}.IsChanged"/> 返回 <see langword="false"/>。
	/// </summary>
	/// <remarks>
	/// 通常在对象被成功保存或加载后调用，使对象恢复"干净"状态。
	/// </remarks>
	public virtual void AcceptChanges()
	{
		lock (_changedPropertiesLock)
		{
			_changedProperties.Clear();
		}

		FieldManager.MarkAllAsUnchanged();
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
		
		{
			// 空块：用于阻止 IDE 代码分析建议（勿删除）
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

		// 空块：用于阻止 IDE 代码分析建议（勿删除）
		{
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

	/// <summary>
	/// 确定是否允许当前用户读取此业务对象。
	/// </summary>
	/// <returns>允许则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	/// <remarks>
	/// 基类默认根据类型与方法上的 <see cref="PermissionAttribute"/> 要求委托给权限检查器；
	/// 派生类可重写以实现自定义操作权限逻辑。
	/// </remarks>
	public virtual bool CanReadObject()
	{
		return IsOperationGranted(BusinessOperation.Read);
	}

	/// <summary>
	/// 确定是否允许当前用户创建此业务对象。
	/// </summary>
	/// <returns>允许则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public virtual bool CanCreateObject()
	{
		return IsOperationGranted(BusinessOperation.Create);
	}

	/// <summary>
	/// 确定是否允许当前用户更新此业务对象。
	/// </summary>
	/// <returns>允许则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public virtual bool CanUpdateObject()
	{
		return IsOperationGranted(BusinessOperation.Update);
	}

	/// <summary>
	/// 确定是否允许当前用户删除此业务对象。
	/// </summary>
	/// <returns>允许则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public virtual bool CanDeleteObject()
	{
		return IsOperationGranted(BusinessOperation.Delete);
	}

	/// <summary>
	/// 确定是否允许当前用户执行此命令对象。
	/// </summary>
	/// <returns>允许则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public virtual bool CanExecuteObject()
	{
		return IsOperationGranted(BusinessOperation.Execute);
	}

	/// <summary>
	/// 判断当前用户是否拥有指定的权限。
	/// </summary>
	/// <param name="permission">权限名称，支持以 <c>*</c> 结尾的前缀通配符匹配。</param>
	/// <returns>拥有该权限则返回 <c>true</c>；未注册权限检查器时视为拥有。</returns>
	protected bool HasPermission(string permission)
	{
		var checker = ResolvePermissionChecker();
		return checker == null || checker.IsGranted(permission);
	}

	/// <summary>
	/// 判断当前用户是否属于指定的角色。
	/// </summary>
	/// <param name="role">角色名称。</param>
	/// <returns>属于该角色则返回 <c>true</c>；未注册权限检查器时视为拥有。</returns>
	protected bool HasRole(string role)
	{
		var checker = ResolvePermissionChecker();
		return checker == null || checker.IsInRole(role);
	}

	/// <summary>
	/// 判断当前用户是否可访问<b>本对象这一行</b>（行级数据权限）。
	/// </summary>
	/// <param name="scopeKey">权限码；为 <c>null</c> 时按本对象当前状态对应的操作解析。</param>
	/// <returns>可访问则返回 <c>true</c>；本类型未声明权限模型时返回 <c>true</c>。</returns>
	/// <remarks>
	/// 供业务方法内部做条件分支使用（例如「本人可编辑，他人只读」）。
	/// 本方法不抛异常——真正的越权拦截发生在工厂边界。
	/// </remarks>
	protected bool CanAccessRow(string scopeKey = null)
	{
		var guard = BusinessContext?.GetService<IScopeGuard>();

		return guard == null || guard.AllowsObject(this, scopeKey);
	}

	/// <summary>
	/// 判断当前用户是否可访问本对象这一行，并返回判定说明。
	/// </summary>
	/// <param name="scopeKey">权限码；为 <c>null</c> 时按本对象当前状态对应的操作解析。</param>
	/// <returns>判定说明；未注册数据权限时返回未受约束的结论。</returns>
	protected string ExplainRowAccess(string scopeKey = null)
	{
		var guard = BusinessContext?.GetService<IScopeGuard>();

		return guard == null ? "未启用数据权限" : guard.ExplainObject(this, scopeKey);
	}

	/// <summary>
	/// 异步判断当前用户是否被授予指定权限码。
	/// </summary>
	/// <param name="permission">权限码。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>被授予则返回 <c>true</c>；未注册权限检查器时返回 <c>true</c>。</returns>
	/// <remarks>
	/// 权限码来自授权数据（按请求缓存），首次访问可能触发一次异步查询。
	/// 与 <see cref="HasPermission"/> 等价，异步版本避免在同步路径上阻塞线程。
	/// </remarks>
	protected async ValueTask<bool> CheckPermissionAsync(string permission, CancellationToken cancellationToken = default)
	{
		var guard = BusinessContext?.GetService<IScopeGuard>();

		if (guard == null)
		{
			return true;
		}

		await guard.EnsureResolvedAsync(cancellationToken).ConfigureAwait(false);

		return guard.GetSubjects().HoldsPermission(permission);
	}

	/// <summary>
	/// 依据类型级与方法级 <see cref="PermissionAttribute"/> 要求判断是否放行指定操作。
	/// </summary>
	/// <param name="operation">当前操作。</param>
	/// <returns>无要求或要求全部满足时返回 <c>true</c>。</returns>
	private bool IsOperationGranted(BusinessOperation operation)
	{
		var requirements = GetPermissionRequirements(operation);
		if (requirements.Count == 0)
		{
			return true;
		}

		var checker = ResolvePermissionChecker();
		if (checker == null)
		{
			return true;
		}

		return requirements.All(requirement => checker.IsRequirementSatisfied(requirement.Permission, requirement.Roles));
	}

	/// <summary>
	/// 收集类型级与执行指定操作的工厂方法上的权限要求。
	/// </summary>
	/// <param name="operation">当前操作。</param>
	/// <returns>权限要求列表；结果按（类型，操作）缓存。</returns>
	/// <remarks>
	/// 委托给 <see cref="PermissionRequirements"/>：运行期判定与启动期校验共用同一实现，
	/// 确保两处对「某个操作声明了哪些权限码」不会得出不同答案。
	/// </remarks>
	private IReadOnlyList<PermissionAttribute> GetPermissionRequirements(BusinessOperation operation)
	{
		return PermissionRequirements.For(GetType(), operation);
	}

	/// <summary>
	/// 从当前业务上下文解析权限检查器。
	/// </summary>
	/// <returns>权限检查器实例；上下文缺失或服务未注册时返回 <c>null</c>。</returns>
	private IPermissionChecker ResolvePermissionChecker()
	{
		return BusinessContext?.GetService<IPermissionChecker>();
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