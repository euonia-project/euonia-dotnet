using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Reflection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务对象的公共根：属性、规则、业务上下文、权限与装载能力。
/// </summary>
/// <remarks>
/// ObjectBase 实现了属性更改通知（INotifyPropertyChanged、INotifyPropertyChanging）、
/// 规则检查（IHasRuleCheck）和资源管理（IDisposable）的接口。派生类应重写相关方法以
/// 实现自定义业务逻辑和验证规则。该类管理规则检查、属性存储、业务上下文与装载作用域；
/// <b>变更追踪</b>由 <see cref="BusinessObject{T}"/> 提供，服务对象不参与。
/// </remarks>
public abstract class ObjectBase : IOsbaObject, IHasRuleCheck, IObjectLifecycle, IDisposable
{
	/// <summary>
	/// 保护规则/字段管理器惰性初始化与校验状态读写的同步锁。
	/// </summary>
	/// <remarks>
	/// <b>变更追踪</b>（已更改属性列表、脏状态）在泛型 <see cref="ObjectBase{T}"/> 中，
	/// 不在这里：服务对象只保留属性与规则能力，不应背负数仓对象的生命周期语义。
	/// </remarks>
	private readonly Lock _sync = new();

	/// <summary>
	/// 惰性创建的业务规则集合。
	/// </summary>
	private Rules _rules;

	/// <summary>
	/// 惰性创建的字段管理器。
	/// </summary>
	private FieldDataManager _fieldManager;

	/// <summary>
	/// 当前对象解析到的规则管理器；由业务上下文决定是容器级还是进程级默认。
	/// </summary>
	private RuleManager _ruleManager;

	/// <summary>
	/// 校验状态；用整数保存以支持无锁读写。
	/// </summary>
	private int _validationState = (int)ValidationState.NotValidated;

	/// <summary>
	/// 获取当前对象的校验状态。
	/// </summary>
	/// <remarks>
	/// <see cref="ValidationState.NotValidated"/> 表示“没有结论”：首次校验之前，
	/// 或对象在最近一次完整校验后又发生了属性变更。
	/// </remarks>
	public ValidationState ValidationState => (ValidationState)Volatile.Read(ref _validationState);

	/// <summary>
	/// 装载作用域计数（按异步流隔离）；工厂方法执行期间允许写入只读对象。
	/// </summary>
	private AsyncLocal<int> _loadScope;

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
			// 上下文可能从“无”变为“容器作用域”：重新解析规则注册表。
			Volatile.Write(ref _ruleManager, null);
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
	/// 暂停所有规则检查，稍后可恢复。
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
	/// <remarks>
	/// 首次检查之前恒为 <see langword="true"/>（违规集合尚未建立）。读取本属性<b>不会</b>
	/// 创建规则集合，因此对从不校验的对象没有额外分配。
	/// </remarks>
	public virtual bool IsValid => _rules?.IsValid ?? true;

	/// <summary>
	/// 获取此业务对象的规则对象。
	/// </summary>
	/// <remarks>
	/// 惰性初始化在临界区内完成：无锁的双重创建会让并发首访产生两个 <see cref="Rules"/> 实例，
	/// 后写者胜出、先者上累积的 <see cref="Rules.BrokenRules"/> 与 <see cref="Rules.RunningRules"/>
	/// 被整体丢弃——表现为偶发「违规列表为空但对象实际不合法」。锁复用既有的
	/// <see cref="_sync"/>（该锁只护各自字段的短临界区，锁序无环）。
	/// </remarks>
	protected Rules Rules
	{
		get
		{
			lock (_sync)
			{
				if (_rules == null)
				{
					_rules = new Rules(this);
				}
				else if (_rules.Target == null)
				{
					_rules.SetTarget(this);
				}

				return _rules;
			}
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
	/// 获取当前对象是否有正在运行的规则；不会创建规则实例。
	/// </summary>
	protected bool HasRunningRules => _rules?.HasRunningRules ?? false;

	/// <summary>
	/// 获取当前对象是否已创建字段管理器；不会创建实例。
	/// </summary>
	protected internal FieldDataManager FieldManagerOrNull => Volatile.Read(ref _fieldManager);

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
		await Rules.CheckObjectRulesAsync(cascade, cancellationToken).ConfigureAwait(false);
		RefreshValidationState(completed: !Rules.IsRuleCheckingSuspended);
		return IsValid;
	}

	/// <summary>
	/// 运行对象级规则检查，存在 Error 级违规时抛出
	/// <see cref="Nerosoft.Euonia.Validation.ValidationException"/>。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// 与保存、操作执行共用同一套检查与异常形态（见 <see cref="ObjectRuleGuard"/>）。
	/// 规则检查被挂起（<see cref="SuspendRuleChecking"/>）时不给出结论、也不抛出。
	/// </remarks>
	/// <exception cref="Nerosoft.Euonia.Validation.ValidationException">存在 Error 级违规时抛出。</exception>
	public async Task EnsureValidAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			await Rules.EnsureObjectRulesAsync(cascade: true, "Object not valid.", cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			RefreshValidationState(completed: !Rules.IsRuleCheckingSuspended);
		}
	}

	/// <summary>
	/// 落库（保存 / 操作执行）前的完整校验：<b>先属性级，再对象级</b>。
	/// </summary>
	/// <param name="message">验证异常的说明。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// <para>
	/// 顺序是刻意的，而且<b>不能颠倒</b>：对象级那一遍一旦发现 Error 就<b>抛异常</b>，
	/// 排在它之后的属性级检查根本没机会执行——字段级错误会被整批丢掉，调用方只能看到一个
	/// 笼统的「对象不合法」。先跑属性级还顺带让错误列表是「先字段、后整体」，
	/// 前端可以直接按这个顺序渲染字段级提示。
	/// </para>
	/// <para>
	/// 之所以先字段后整体在语义上也说得通：<b>对象级规则可能依赖一个或多个属性值</b>，
	/// 让它在「字段都已过检」的前提下判整体更自然。
	/// </para>
	/// <para>
	/// 属性级那一遍<b>只在 <see cref="CheckRuleOnPropertyChanged"/> 为 <see langword="false"/> 时执行</b>：
	/// 那种类型在 setter 上不做检查（推迟到这里），因此需要在这里补上；而默认模式下属性级规则
	/// 已在变更时跑过，其结果就在违规集合里，这里再跑一遍纯属重复。
	/// </para>
	/// <para>
	/// 补跑时<b>只检查变更过的属性</b>（<see cref="BusinessObject{T}.ChangedProperties"/>）：它们才是本次要落库的字段，
	/// 未修改（乃至未装载）的属性不参与校验，也就不会因为「读出来是默认值」而被凭空拦下。
	/// </para>
	/// <para>
	/// 那一遍属性级检查对服务对象自然为空：<see cref="ServiceObject{T}"/> 是<b>无状态</b>的
	/// （只表达「要执行某种操作」，不持有属性），没有变更追踪，
	/// 它也不参与变更追踪。命令的校验一律写成<b>对象级</b>规则，对象级那一遍照常执行。
	/// </para>
	/// </remarks>
	/// <exception cref="Nerosoft.Euonia.Validation.ValidationException">存在 Error 级违规时抛出。</exception>
	internal async Task EnsureRulesAsync(string message, CancellationToken cancellationToken = default)
	{
		try
		{
			if (!CheckRuleOnPropertyChanged)
			{
				await Rules.CheckRulesAsync(RulesPropertiesToCheck, cancellationToken).ConfigureAwait(false);
			}

			await Rules.EnsureObjectRulesAsync(cascade: true, message, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			RefreshValidationState(completed: !Rules.IsRuleCheckingSuspended);
		}
	}

	/// <summary>
	/// 异步检查指定属性的规则。
	/// </summary>
	/// <param name="property">目标属性。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// <para>
	/// 与 <see cref="CheckPropertyRules(IPropertyInfo)"/>（同步、阻塞、供 setter 热路径）相对：
	/// 本方法在<b>异步流程里</b> <c>await</c> 属性级规则——含 I/O 的校验（查库、调远端）
	/// 不会阻塞线程池线程。典型用法是 <c>CheckRuleOnPropertyChanged =&gt; false</c> 的类型
	/// 在保存前的异步管线里逐属性校验；「推迟到保存时」的路径
	/// （<see cref="EnsureRulesAsync(string, CancellationToken)"/>）内部走的就是同一异步内核。
	/// </para>
	/// <para>
	/// 与保存路径的差别：本方法<b>只跑指定属性</b>、不清对象级违规、不抛
	/// <see cref="Nerosoft.Euonia.Validation.ValidationException"/>——结论仍经 <see cref="IsValid"/> 读取。
	/// </para>
	/// </remarks>
	internal async Task CheckPropertyRulesAsync(IPropertyInfo property, CancellationToken cancellationToken = default)
	{
		await Rules.CheckRulesAsync(property, cancellationToken).ConfigureAwait(false);
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
	/// 按当前违规集合刷新校验状态。
	/// </summary>
	/// <param name="completed">是否完成了一轮完整检查；挂起时传 <see langword="false"/>。</param>
	private void RefreshValidationState(bool completed)
	{
		if (!completed)
		{
			SetValidationState(ValidationState.NotValidated);
			return;
		}

		SetValidationState(IsValid ? ValidationState.Valid : ValidationState.Invalid);
	}

	/// <summary>
	/// 设置校验状态。
	/// </summary>
	/// <param name="state">状态。</param>
	protected void SetValidationState(ValidationState state)
	{
		Volatile.Write(ref _validationState, (int)state);
	}

	/// <summary>
	/// 解析当前对象应使用的规则管理器：有业务上下文时使用容器级 <see cref="IRuleRegistry"/>，
	/// 否则回退到进程级默认注册表。
	/// </summary>
	/// <returns>规则管理器。</returns>
	internal RuleManager GetRuleManager()
	{
		var manager = Volatile.Read(ref _ruleManager);
		if (manager != null)
		{
			return manager;
		}

		var resolved = ResolveRuleManager();
		return Interlocked.CompareExchange(ref _ruleManager, resolved, null) ?? resolved;
	}

	/// <summary>
	/// 从业务上下文解析规则注册表。
	/// </summary>
	/// <returns>规则管理器。</returns>
	private RuleManager ResolveRuleManager()
	{
		var registry = BusinessContext?.GetService<IRuleRegistry>();
		return registry?.GetRules(GetType()) ?? RuleManager.GetRules(GetType());
	}

	/// <summary>
	/// 初始化当前类型的验证规则，确保在使用前设置好所有必需的规则。
	/// </summary>
	/// <remarks>此方法线程安全，可防止同一类型的规则被并发初始化。如果初始化期间发生错误，
	/// 任何部分初始化的规则都会被清理以保持一致性。在依赖类型验证规则的操作之前调用此方法。</remarks>
	private void InitializeRules()
	{
		var rules = GetRuleManager();
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
	/// 向当前上下文添加验证规则。派生类应重写此方法以指定自定义验证逻辑。
	/// </summary>
	/// <remarks>
	/// 实现应确保添加所有必要的规则以保持数据完整性。此方法在验证过程的初始化阶段被调用。
	/// </remarks>
	protected virtual void AddRules()
	{
	}

	/// <summary>
	/// 获取当前对象的规则管理器。
	/// </summary>
	/// <returns>规则管理器。</returns>
	protected RuleManager GetRegisteredRules()
	{
		return Rules.RuleManager;
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
	/// 获取一个值，指示属性变更时是否触发该属性的规则检查。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 默认为 <see langword="true"/>——这是属性级规则的设计原则：<b>属性级规则在属性变更时检查</b>，
	/// 于是只有真正被修改过的属性会被校验。未修改（乃至未装载）的属性不参与校验，
	/// 保存时也就不会因为「读出来是默认值」而被凭空拦下；
	/// 对应地，<b>未修改的属性不应回写数据库</b>（持久化只取 <see cref="BusinessObject{T}.ChangedProperties"/>）。
	/// </para>
	/// <para>
	/// 校验产生的违规会按属性归因落进 <see cref="GetBrokenRules"/>，因此保存时仍会经
	/// <see cref="IsValid"/> 生效并抛 <see cref="Nerosoft.Euonia.Validation.ValidationException"/>。
	/// </para>
	/// <para>
	/// 只有该属性<b>存在规则</b>时才进入规则检查（<c>Rules.HasRulesFor</c> 短路）；
	/// 没有规则的属性走普通变更通知，不产生额外开销。
	/// </para>
	/// <para>
	/// 关心性能、或属性级规则需要 I/O 的类型可以覆写为 <see langword="false"/>：
	/// 该路径是<b>同步阻塞</b>调用线程的（见 <c>Rules.RunPropertyRules</c>），
	/// 异步/耗时的校验请放对象级规则（保存走异步路径）。
	/// </para>
	/// </remarks>
	protected internal virtual bool CheckRuleOnPropertyChanged => true;

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
		// 无订阅者时不构造事件参数：属性 setter 是热路径，零订阅场景应零分配。
		var handler = PropertyChanged;
		if (handler != null)
		{
			handler(this, new PropertyChangedEventArgs(propertyName));
		}

		NotifyParents(propertyName);
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
	/// 为指定属性和值引发 PropertyChanged 事件。
	/// </summary>
	/// <param name="name">属性名。</param>
	/// <param name="value">属性值。</param>
	protected virtual void OnPropertyChanged(string name, object value)
	{
		OnPropertyChanged(name);
	}

	/// <summary>
	/// 通知属性值即将更改。
	/// </summary>
	/// <param name="propertyName">即将更改的属性的名称。</param>
	protected virtual void OnPropertyChanging(string propertyName)
	{
		var handler = PropertyChanging;
		if (handler != null)
		{
			handler(this, new PropertyChangingEventArgs(propertyName));
		}
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
	/// 将指定属性标记为脏的或已更改。
	/// </summary>
	/// <param name="property">要标记的属性信息。</param>
	protected virtual void PropertyHasChanged(IPropertyInfo property)
	{
		// 公共基类不做变更追踪：服务对象等“操作对象”没有对象生命周期。
		// 需要追踪的派生类（BusinessObject<T>）重写本方法。
		SetValidationState(ValidationState.NotValidated);
		OnPropertyChanged(property);
	}

	/// <summary>
	/// 判断属性是否有可能适用的规则；不会创建本对象的 <see cref="Rules"/> 实例。
	/// </summary>
	/// <param name="property">属性。</param>
	/// <returns>存在类型级、实例级或操作级规则时返回 <see langword="true"/>。</returns>
	private protected bool HasAnyRulesFor(IPropertyInfo property)
	{
		if (GetRuleManager().RulesFor(property).Count > 0)
		{
			return true;
		}

		var rules = _rules;
		return rules != null && rules.HasInstanceRulesFor(property);
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
	/// 获取保存前需要补跑属性级规则的属性集合。
	/// </summary>
	/// <remarks>
	/// 只有带变更追踪的对象（<see cref="BusinessObject{T}"/>）才有“本次变更过的属性”；
	/// 服务对象没有变更追踪，因此默认为空。属性级规则在 setter 上触发，
	/// 服务对象应使用对象级规则。
	/// </remarks>
	protected virtual IReadOnlyList<IPropertyInfo> RulesPropertiesToCheck => Array.Empty<IPropertyInfo>();

	/// <summary>
	/// 提交当前更改：使字段的修改历史失效，
	/// 将字段当前值作为新的基线，令 <see cref="BusinessObject{T}.HasChangedProperties"/> 和字段的
	/// <see cref="FieldData{T}.IsChanged"/> 返回 <see langword="false"/>。
	/// </summary>
	/// <remarks>
	/// 通常在对象被成功保存或加载后调用，使对象恢复"干净"状态。
	/// </remarks>
	public virtual void AcceptChanges()
	{
		_fieldManager?.MarkAllAsUnchanged();

		// 干净对象还没有本轮校验结论；需要结论请调用 ValidateAsync/EnsureValidAsync。
		SetValidationState(ValidationState.NotValidated);
	}

	#endregion

	#region Lifecycle callbacks

	/// <inheritdoc />
	Task IObjectLifecycle.OnSavingAsync(CancellationToken cancellationToken)
	{
		return OnSavingAsync(cancellationToken);
	}

	/// <inheritdoc />
	Task IObjectLifecycle.OnSavedAsync(object result, CancellationToken cancellationToken)
	{
		return OnSavedAsync(result, cancellationToken);
	}

	/// <inheritdoc />
	Task IObjectLifecycle.OnExecutingAsync(CancellationToken cancellationToken)
	{
		return OnExecutingAsync(cancellationToken);
	}

	/// <inheritdoc />
	Task IObjectLifecycle.OnExecutedAsync(CancellationToken cancellationToken)
	{
		return OnExecutedAsync(cancellationToken);
	}

	/// <summary>
	/// 保存前回调；默认无操作。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步回调的任务。</returns>
	/// <remarks>在规则校验通过之后、工厂方法执行之前调用。</remarks>
	protected virtual Task OnSavingAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 保存成功后回调；默认无操作。
	/// </summary>
	/// <param name="result">工厂返回的保存结果；可能与当前实例不同。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步回调的任务。</returns>
	protected virtual Task OnSavedAsync(object result, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 执行体执行前回调；默认无操作。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步回调的任务。</returns>
	/// <remarks>在授权与规则校验通过之后、执行体之前调用。</remarks>
	protected virtual Task OnExecutingAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 执行体执行成功后回调；默认无操作。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步回调的任务。</returns>
	protected virtual Task OnExecutedAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	#endregion

	#region Property Checks

	/// <summary>
	/// 绕过规则检查的异步流作用域开关。
	/// </summary>
	/// <remarks>
	/// 绕过状态存放在 <see cref="AsyncLocal{T}"/> 中，只影响发起它的逻辑流及其派生流；
	/// 并发流互不可见，<c>using</c> 退出时恢复本流进入前的值。
	/// 父对象的绕过会沿父子链向下穿透到子对象。
	/// </remarks>
	protected virtual bool IsBypassingRuleChecks
	{
		get
		{
			if (OwnBypassRuleChecks || Volatile.Read(ref _hasParents) == 0)
			{
				return OwnBypassRuleChecks;
			}

			var parents = GetParentLinks();
			if (parents.Length == 0)
			{
				return false;
			}

			// 防御对象图里意外的环：超过深度后按“不绕过”处理。
			if (_bypassTraversalDepth >= MaxBypassTraversalDepth)
			{
				return false;
			}

			_bypassTraversalDepth++;
			try
			{
				foreach (var link in parents)
				{
					if (link.Parent.IsBypassingRuleChecks)
					{
						return true;
					}
				}
			}
			finally
			{
				_bypassTraversalDepth--;
			}

			return false;
		}
		set => OwnBypassRuleChecks = value;
	}

	/// <summary>
	/// 父对象链的遍历深度上限，避免异常对象图（环）导致栈溢出。
	/// </summary>
	private const int MaxBypassTraversalDepth = 32;

	/// <summary>
	/// 当前线程的父链遍历深度。
	/// </summary>
	[ThreadStatic]
	private static int _bypassTraversalDepth;

	/// <summary>
	/// 本对象作为子对象时登记的父对象链接。
	/// </summary>
	private List<ChildLink> _parentLinks;

	/// <summary>
	/// 父链接数量；用于热路径上无锁判断“本对象是否是子对象”。
	/// </summary>
	private int _hasParents;

	/// <summary>
	/// 保护 <see cref="_parentLinks"/> 的同步锁。
	/// </summary>
	private readonly Lock _parentLock = new();

	/// <summary>
	/// 登记父对象及其持有本对象的属性；重复登记按引用去重。
	/// </summary>
	/// <param name="parent">父对象。</param>
	/// <param name="property">父对象上持有本对象的属性。</param>
	internal void AddParent(ObjectBase parent, IPropertyInfo property)
	{
		if (parent == null || ReferenceEquals(parent, this))
		{
			return;
		}

		lock (_parentLock)
		{
			_parentLinks ??= [];
			foreach (var link in _parentLinks)
			{
				if (ReferenceEquals(link.Parent, parent) && ReferenceEquals(link.Property, property))
				{
					return;
				}
			}

			_parentLinks.Add(new ChildLink(parent, property));
			Volatile.Write(ref _hasParents, _parentLinks.Count);
		}
	}

	/// <summary>
	/// 移除父对象链接。
	/// </summary>
	/// <param name="parent">父对象。</param>
	/// <param name="property">父对象上持有本对象的属性。</param>
	internal void RemoveParent(ObjectBase parent, IPropertyInfo property)
	{
		lock (_parentLock)
		{
			if (_parentLinks == null)
			{
				return;
			}

			for (var index = _parentLinks.Count - 1; index >= 0; index--)
			{
				var link = _parentLinks[index];
				if (ReferenceEquals(link.Parent, parent) && ReferenceEquals(link.Property, property))
				{
					_parentLinks.RemoveAt(index);
					Volatile.Write(ref _hasParents, _parentLinks.Count);
					return;
				}
			}
		}
	}

	/// <summary>
	/// 取父链快照；无父对象时返回空数组。
	/// </summary>
	/// <returns>父链快照。</returns>
	private ChildLink[] GetParentLinks()
	{
		if (Volatile.Read(ref _hasParents) == 0)
		{
			return Array.Empty<ChildLink>();
		}

		lock (_parentLock)
		{
			return _parentLinks is { Count: > 0 } ? [.. _parentLinks] : Array.Empty<ChildLink>();
		}
	}

	/// <summary>
	/// 把子对象变更通知给所有父对象。
	/// </summary>
	/// <param name="propertyName">子对象上发生变化的属性名。</param>
	private void NotifyParents(string propertyName)
	{
		if (Volatile.Read(ref _hasParents) == 0)
		{
			return;
		}

		var links = GetParentLinks();
		if (links.Length == 0)
		{
			return;
		}

		// 防止父子双向引用形成通知环。
		var notifying = _notifyingParents ??= [];
		if (!notifying.Add(this))
		{
			return;
		}

		try
		{
			foreach (var link in links)
			{
				link.Parent.OnChildPropertyChanged(link.Property, this, propertyName);
			}
		}
		finally
		{
			notifying.Remove(this);
			if (notifying.Count == 0)
			{
				_notifyingParents = null;
			}
		}
	}

	/// <summary>
	/// 当前通知链上的对象集合，用于阻断父子双向引用形成的环。
	/// </summary>
	[ThreadStatic]
	private static HashSet<ObjectBase> _notifyingParents;

	/// <summary>
	/// 子对象属性变化时的父对象回调：默认把父对象上持有该子对象的属性标脏并通知。
	/// </summary>
	/// <param name="property">父对象上持有子对象的属性。</param>
	/// <param name="child">发生变化的子对象。</param>
	/// <param name="childPropertyName">子对象上发生变化的属性名。</param>
	protected virtual void OnChildPropertyChanged(IPropertyInfo property, ObjectBase child, string childPropertyName)
	{
		PropertyHasChanged(property);
	}

	/// <summary>
	/// 供子容器（如 <see cref="ObservableList{T}"/> / <see cref="ObservableDictionary{TKey,TValue}"/>）通知父对象：容器内容发生变化。
	/// </summary>
	/// <param name="property">父对象上持有容器的属性。</param>
	internal void NotifyChildChanged(IPropertyInfo property)
	{
		PropertyHasChanged(property);
	}

	/// <summary>
	/// 建立/解除父子链接：用于子对象属性变更通知与规则绕过传播。
	/// </summary>
	/// <param name="property">父对象上的属性。</param>
	/// <param name="oldValue">旧值。</param>
	/// <param name="newValue">新值。</param>
	protected void AttachChild(IPropertyInfo property, object oldValue, object newValue)
	{
		if (ReferenceEquals(oldValue, newValue))
		{
			return;
		}

		if (oldValue is ObjectBase oldChild)
		{
			oldChild.RemoveParent(this, property);
		}
		else if (oldValue is IChildGraphNode oldNode)
		{
			oldNode.RemoveParent(this, property);
		}

		if (newValue is ObjectBase newChild)
		{
			newChild.AddParent(this, property);
		}
		else if (newValue is IChildGraphNode newNode)
		{
			newNode.AddParent(this, property);
		}
	}

	/// <summary>
	/// 父子链接：父对象 + 父对象上持有子对象的属性。
	/// </summary>
	private sealed class ChildLink(ObjectBase parent, IPropertyInfo property)
	{
		/// <summary>父对象。</summary>
		public ObjectBase Parent { get; } = parent;

		/// <summary>父对象上持有子对象的属性。</summary>
		public IPropertyInfo Property { get; } = property;
	}

	/// <summary>
	/// 本实例的绕过开关容器；<see langword="null"/> 表示从未在本流的任何位置置位（读侧零分配）。
	/// </summary>
	/// <remarks>
	/// 状态按<b>实例</b>隔离，流内传播由 <see cref="AsyncLocal{T}"/> 自己负责，因此这里不加
	/// <c>[ThreadStatic]</c>：该特性只对静态字段生效，写在实例字段上既无效，又会让人误以为存在线程亲和。
	/// </remarks>
	private AsyncLocal<bool> _bypassScope;

	/// <summary>
	/// 获取本实例自身的绕过值（不含父对象传播）。
	/// </summary>
	private bool OwnBypassRuleChecks
	{
		get => _bypassScope?.Value ?? false;
		set
		{
			_bypassScope ??= new AsyncLocal<bool>();
			_bypassScope.Value = value;
		}
	}

	/// <summary>
	/// 通过将此属性包裹在 Using 块中，可以在不引发 PropertyChanged 事件
	/// 和不检查用户权限的情况下，为当前业务对象设置属性值。
	/// </summary>
	/// <remarks>
	/// 每次访问都返回独立的流级作用域对象：绕过标志存放在 AsyncLocal 中，
	/// 共享管理器无法为并发流分别设置/还原各自的值。
	/// </remarks>
	protected internal BypassRuleChecksObject BypassRuleChecks => new(this);

	/// <summary>
	/// 用于创建绕过规则检查的对象，允许设置某些即使不是严格有效的值。
	/// 该对象还允许开发者在任何时候检查某些规则是否正在被绕过。
	/// </summary>
	/// <remarks>
	/// <see cref="Dispose"/> 还原的是<b>进入时捕获的本流旧值</b>而不是恒 <see langword="false"/>：
	/// 嵌套 <c>using</c>（例如 lambda 规则内再嵌一层绕过）退出时不会关闭外层的绕过状态。
	/// </remarks>
	protected internal sealed class BypassRuleChecksObject : IDisposable
	{
		private ObjectBase _target;
		private readonly bool _captured;
		private bool _disposed;

		/// <summary>
		/// 在当前异步流中开启规则检查绕过，并捕获进入时的旧值以便还原。
		/// </summary>
		/// <param name="target">要绕过规则检查的业务对象。</param>
		internal BypassRuleChecksObject(ObjectBase target)
		{
			_target = target;

			// 只捕获/恢复“自身”的绕过值：父对象传播的绕过不属于本实例，
			// 否则父对象退出绕过时，子对象会被错误地永久置为绕过。
			_captured = target.OwnBypassRuleChecks;
			_target.OwnBypassRuleChecks = true;
		}

		#region IDisposable Members

		/// <summary>
		/// 还原进入本流时的旧值。
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			// AsyncLocal 的值属于当前异步流，每个 using 实例独立还原即可，
			// 不需要也不应在并发流之间共享引用计数。
			_target.IsBypassingRuleChecks = _captured;
			_target = null;
		}

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
	/// <remarks>
	/// 惰性初始化在临界区内完成：无锁的双重创建会让并发首访产生两个
	/// <see cref="FieldDataManager"/>，两份 <c>_fieldData</c> 各自维护变更历史——
	/// <see cref="BusinessObject{T}.HasChangedProperties"/> 与 <see cref="ReadProperty{TValue}(PropertyInfo{TValue})"/> 会在
	/// 线程间给出分歧答案。锁复用 <see cref="_sync"/>，与 <see cref="Rules"/> 同一口径。
	/// </remarks>
	public FieldDataManager FieldManager
	{
		get
		{
			var manager = Volatile.Read(ref _fieldManager);
			if (manager != null)
			{
				return manager;
			}

			lock (_sync)
			{
				return _fieldManager ??= new FieldDataManager(GetType());
			}
		}
	}

	#endregion

	#region Read Properties

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
			AttachChild(propertyInfo, null, result);
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
			AttachChild(propertyInfo, null, result);
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
			throw new InvalidOperationException(string.Format(Resources.IDS_PROPERTY_NOT_REGISTERED, propertyName));
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
			throw new InvalidOperationException(string.Format(Resources.IDS_PROPERTY_NOT_REGISTERED, propertyName));
		}

		if (propertyInfo is not PropertyInfo<TValue> property)
		{
			throw new InvalidOperationException(string.Format(Resources.IDS_PROPERTY_TYPE_MISMATCH, propertyName, propertyInfo.Type.Name, typeof(TValue).Name));
		}

		{
			// 空块：用于阻止 IDE 代码分析建议（勿删除）
		}

		return ReadProperty(property);
	}

	/// <summary>
	/// 读取指定属性并转换为目标类型。
	/// </summary>
	/// <typeparam name="TValue">字段的类型。</typeparam>
	/// <typeparam name="TProperty">属性的类型。</typeparam>
	/// <param name="propertyInfo">属性元数据。</param>
	/// <returns>转换后的属性值。</returns>
	protected TProperty ReadPropertyConvert<TValue, TProperty>(PropertyInfo<TValue> propertyInfo)
	{
		return TypeHelper.CoerceValue<TProperty>(typeof(TValue), ReadProperty(propertyInfo));
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
		EnsureCanLoadProperty(propertyInfo);

		var oldValue = FieldManager.GetExistingOrInit(propertyInfo);

		LoadPropertyValue(propertyInfo, oldValue, newValue, false);
	}

	/// <summary>
	/// 开始一次装载作用域：期间允许通过 <see cref="LoadProperty(IPropertyInfo, object)"/> 写入
	/// 通常拒绝写入的对象（例如只读对象）。工厂方法在调用业务方法时自动进入该作用域。
	/// </summary>
	/// <returns>作用域；释放后恢复进入前的状态。</returns>
	/// <remarks>
	/// 作用域按异步流隔离（<see cref="AsyncLocal{T}"/>），并发调用互不可见。
	/// 这是<b>装载</b>入口，不是用户写入入口：它不会标记变更、不会触发规则。
	/// </remarks>
	protected internal IDisposable BeginLoad()
	{
		var scope = Volatile.Read(ref _loadScope) ?? Interlocked.CompareExchange(ref _loadScope, new AsyncLocal<int>(), null) ?? _loadScope;
		return new LoadScope(scope);
	}

	/// <summary>
	/// 获取当前异步流是否处于装载作用域。
	/// </summary>
	protected internal bool IsLoading => (_loadScope?.Value ?? 0) > 0;

	/// <summary>
	/// 判断当前是否允许装载指定属性；只读对象默认拒绝，除非处于装载作用域。
	/// </summary>
	/// <param name="property">属性。</param>
	/// <returns>允许装载则返回 <see langword="true"/>。</returns>
	protected internal virtual bool CanLoadProperty(IPropertyInfo property)
	{
		// 装载不是用户写入：工厂作用域内一律允许；作用域外退回“能否写入”的判定，
		// 使限制了某些字段写入的派生对象不会因为 public LoadProperty 而被绕过。
		return IsLoading || CanWriteProperty(property);
	}

	/// <summary>
	/// 在装载前校验写权限；只读对象在非装载作用域下应拒绝。
	/// </summary>
	/// <param name="property">属性。</param>
	/// <exception cref="SecurityException">当前不允许装载该属性时抛出。</exception>
	private void EnsureCanLoadProperty(IPropertyInfo property)
	{
		if (!CanLoadProperty(property))
		{
			throw new SecurityException(string.Format(Resources.IDS_PROPERTY_SET_NOT_ALLOWED, property.Name));
		}
	}

	/// <summary>
	/// 装载作用域：进入时计数 +1，释放时 -1。
	/// </summary>
	private sealed class LoadScope : IDisposable
	{
		private readonly AsyncLocal<int> _scope;
		private bool _disposed;

		/// <summary>
		/// 进入装载作用域。
		/// </summary>
		/// <param name="scope">当前实例的装载计数器。</param>
		public LoadScope(AsyncLocal<int> scope)
		{
			_scope = scope;
			scope.Value++;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_scope.Value--;
		}
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

		AttachChild(propertyInfo, oldValue, newValue);

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
		EnsureCanLoadProperty(propertyInfo);
		_ = LoadPropertyByReflection(nameof(LoadProperty), propertyInfo, newValue);
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
	/// 对于类型实现 IOsbaObject 的属性，此方法使用引用相等性来确定值是否不同。
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
			if (typeof(IOsbaObject).IsAssignableFrom(propertyInfo.Type))
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
			throw new SecurityException(string.Format(Resources.IDS_PROPERTY_GET_NOT_ALLOWED, property.Name));
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

		{
			// 空块：用于阻止 IDE 代码分析建议（勿删除）
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
			throw new SecurityException(string.Format(Resources.IDS_PROPERTY_SET_NOT_ALLOWED, property.Name));
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
	/// 确定当前用户是否被允许对本对象执行指定的业务操作。
	/// </summary>
	/// <param name="operation">操作标识；既包括 <see cref="BusinessOperation"/> 的 5 个内置操作，
	/// 也包括宿主自定义的操作（如 <c>approve</c>、<c>order:archive</c>）。</param>
	/// <returns>允许则返回 <c>true</c>；无权限要求或无从判定时同样返回 <c>true</c>。</returns>
	/// <remarks>
	/// <para>
	/// 这是操作级权限<b>唯一的判定入口</b>，也是派生类唯一的定制点：内置操作与自定义操作一视同仁，
	/// 都按类型级与方法级 <see cref="PermissionAttribute"/> 要求委托给宿主注册的
	/// <see cref="IPermissionChecker"/>。判定逻辑本身归 <c>ObjectAuthorization</c>，
	/// 本方法只是它暴露在对象上的那一面。
	/// </para>
	/// <para>
	/// <b>查询语义</b>：本方法不抛异常，无从判定（未接入 <see cref="BusinessContext"/>、未注册判定实现）
	/// 时返回 <c>true</c>。<b>真正的拦截在工厂边界</b>（<c>ObjectAuthorization.EnsureAuthorized</c>），
	/// 那里有要求却判定不了会抛 <see cref="InvalidOperationException"/>，拒绝会抛
	/// <see cref="SecurityException"/>。业务方法内部做条件分支用本方法。
	/// </para>
	/// </remarks>
	public virtual bool CanPerformOperation(string operation)
	{
		return ObjectAuthorization.IsGranted(this, operation);
	}

	#endregion

	#region IDisposable

	private bool _disposedValue;

	/// <summary>
	/// 可释放模式的实现。本类型当前不持有任何资源，此方法仅维护释放状态，
	/// 供派生类重写并在其中按 <paramref name="disposing"/> 释放资源。
	/// </summary>
	/// <param name="disposing">
	/// <c><see langword="true"/></c> 表示由 <see cref="Dispose()"/> 显式释放（可安全访问托管资源）；
	/// <c><see langword="false"/></c> 表示由终结器释放（不可访问托管资源）。
	/// 本类型没有终结器，故该参数恒为 <c><see langword="true"/></c>。
	/// </param>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue)
		{
			return;
		}

		if (disposing)
		{
			// 断开父子链接：避免子对象继续向已释放的父对象发通知/读取绕过状态。
			var manager = FieldManagerOrNull;
			if (manager != null)
			{
				foreach (var property in manager.GetRegisteredProperties())
				{
					var value = manager.GetFieldData(property)?.Value;
					if (value is ObjectBase child)
					{
						child.RemoveParent(this, property);
					}
					else if (value is IChildGraphNode node)
					{
						node.RemoveParent(this, property);
					}
				}
			}

			lock (_parentLock)
			{
				_parentLinks?.Clear();
				Volatile.Write(ref _hasParents, 0);
			}
		}

		// 派生类引入资源时在此释放：托管资源只应在 disposing 为 true 时访问，非托管资源两种情况都要释放。
		_disposedValue = true;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}
