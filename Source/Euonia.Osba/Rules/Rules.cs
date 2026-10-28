using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ValidationException = Nerosoft.Euonia.Validation.ValidationException;
using ValidationResult = Nerosoft.Euonia.Validation.ValidationResult;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="IRules"/> 接口的实现。
/// </summary>
public class Rules : IRules
{
	private readonly object _lockObject = new();

	/// <summary>
	/// 规则执行完成回调；缓存为实例字段，避免每条规则创建一个闭包。
	/// </summary>
	private readonly Action<IRuleContext> _ruleCompleted;

	/// <summary>
	/// 规则类型 → <see cref="ExecuteOnStateAttribute"/> 状态列表的缓存。
	/// </summary>
	private static readonly ConcurrentDictionary<Type, ObjectEditState[]> _ruleStates = new();

	internal Rules(IHasRuleCheck @object)
	{
		_target = @object;
		_ruleCompleted = OnRuleCompleted;
	}

	private IHasRuleCheck _target;

	/// <inheritdoc />
	public object Target => _target;

	/// <summary>
	/// 获取规则管理器。
	/// </summary>
	internal RuleManager RuleManager
	{
		get
		{
			if (Target is ObjectBase businessObject)
			{
				return businessObject.GetRuleManager();
			}

			return Nerosoft.Euonia.Osba.RuleManager.GetRules(Target.GetType());
		}
	}

	/// <summary>
	/// 获取一个值，指示当前是否存在违规规则，存在则意味着对象无效。
	/// </summary>
	/// <summary>
	/// 获取一个值，指示当前是否存在 Error 级违规。
	/// </summary>
	/// <remarks>
	/// 违规集合按需创建：从未校验过的对象读取 <see cref="IsValid"/> 时不分配集合。
	/// </remarks>
	public bool IsValid => _brokenRules == null || _brokenRules.ErrorCount == 0;

	/// <summary>
	/// 惰性创建的违规集合。
	/// </summary>
	private BrokenRuleCollection _brokenRules;

	/// <summary>
	/// 获取违规集合；首次访问时创建。
	/// </summary>
	internal BrokenRuleCollection BrokenRules => _brokenRules ??= new();

	/// <summary>
	/// 获取或设置一个值，指示是否抑制规则检查。
	/// </summary>
	public bool SuppressRuleChecking { get; set; }

	private List<IRuleBase> RunningRules { get; } = new();

	/// <summary>
	/// 仅对本业务对象实例生效的规则。
	/// </summary>
	/// <remarks>
	/// 与 <see cref="AddRule(IRuleBase)"/> 写入的进程级共享集合不同，实例规则随对象生命周期结束而消失，
	/// 因此适用于按对象指定的规则，不会有跨实例、跨容器甚至跨测试的污染。
	/// 访问一律在 <see cref="_lockObject"/> 内进行。
	/// </remarks>
	private readonly List<IRuleBase> _instanceRules = [];

	/// <summary>
	/// 本次操作挂上的规则范围栈（由执行器在操作开始时挂载、结束时卸下）。
	/// </summary>
	/// <remarks>
	/// 用栈而不是单槽位：嵌套执行器（外层操作内部再启动一个执行器）与同一实例上的并发操作
	/// 都必须各自保留自己的规则范围，先结束的内层不能把外层的范围一并清掉。
	/// 所有访问都在 <see cref="_lockObject"/> 内。
	/// </remarks>
	private readonly List<RuleScope> _operationScopes = [];

	/// <summary>
	/// 操作范围数量；用于在热路径上无锁判断“是否存在操作级规则”。
	/// </summary>
	private int _operationScopeCount;

	/// <summary>
	/// 获取一个值，指示规则检查当前是否被挂起——即「本次不给出结论」。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 两个来源：调用方显式 <see cref="SuppressRuleChecking"/>（<c>SuspendRuleChecking</c>），
	/// 或本次操作声明跳过检查（<c>ActuatorBase.BypassRuleChecks</c>）。两者语义一致，故合并成一个判定入口。
	/// </para>
	/// <para>
	/// <b>挂起不等于「对象有效」</b>：挂起期间既不跑规则、也不读陈旧的 <see cref="BrokenRules"/>——
	/// 后者反映的是上一次检查，据此放行或拦截都是错的（这正是 <c>SuspendRuleChecking</c> 后
	/// 保存会按旧结论抛错的原因）。
	/// </para>
	/// </remarks>
	internal bool IsRuleCheckingSuspended
	{
		get
		{
			if (SuppressRuleChecking)
			{
				return true;
			}

			if (Volatile.Read(ref _operationScopeCount) == 0)
			{
				return false;
			}

			lock (_lockObject)
			{
				foreach (var scope in _operationScopes)
				{
					if (scope.SkipChecks)
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	/// <summary>
	/// 挂上本次操作的规则范围。
	/// </summary>
	/// <param name="scope">规则范围。</param>
	/// <remarks>
	/// 执行器在操作开始前挂载、结束后卸下（见 <c>ActuatorBase.ExecuteAsync</c>）。
	/// 不写入 <see cref="RuleManager"/>，因此并发操作之间互不可见。
	/// </remarks>
	internal void AttachOperationRules(RuleScope scope)
	{
		Check.EnsureNotNull(scope, nameof(scope));

		lock (_lockObject)
		{
			_operationScopes.Add(scope);
			Volatile.Write(ref _operationScopeCount, _operationScopes.Count);
		}
	}

	/// <summary>
	/// 卸下本次操作的规则范围。
	/// </summary>
	/// <param name="scope">要卸下的范围。</param>
	/// <remarks>
	/// 按引用从栈顶向下查找并移除：嵌套/并发场景下先结束的范围不会误删其他范围。
	/// </remarks>
	internal void DetachOperationRules(RuleScope scope)
	{
		if (scope == null)
		{
			return;
		}

		lock (_lockObject)
		{
			for (var index = _operationScopes.Count - 1; index >= 0; index--)
			{
				if (ReferenceEquals(_operationScopes[index], scope))
				{
					_operationScopes.RemoveAt(index);
					Volatile.Write(ref _operationScopeCount, _operationScopes.Count);
					return;
				}
			}
		}
	}

	/// <summary>
	/// 获取当前是否存在实例级或操作级规则。
	/// </summary>
	internal bool HasSupplementalRules => _instanceRules.Count > 0 || Volatile.Read(ref _operationScopeCount) > 0;

	/// <summary>
	/// 获取一个值，指示当前是否有正在运行的规则。
	/// </summary>
	public bool HasRunningRules
	{
		get
		{
			lock (_lockObject)
			{
				return RunningRules.Count > 0;
			}
		}
	}

	internal void SetTarget(IHasRuleCheck target)
	{
		_target = target;
	}

	/// <summary>
	/// 向业务规则管理器添加规则。
	/// </summary>
	/// <param name="rule">要添加的规则。</param>
	/// <remarks>
	/// <b>规则按类型进程级共享</b>：注册一次即对此类型的<b>所有</b>实例、以及同进程内的所有容器生效。
	/// 需要「只对当前对象生效」的规则请改用 <see cref="AddInstanceRule(IRuleBase)"/>。
	/// </remarks>
	public void AddRule(IRuleBase rule)
	{
		RuleManager.Add(rule);
	}

	/// <summary>
	/// 添加一条仅对当前业务对象实例生效的规则。
	/// </summary>
	/// <param name="rule">要添加的规则。</param>
	/// <remarks>
	/// 与 <see cref="AddRule(IRuleBase)"/> 的区别：本方法的规则不进入进程级共享集合，
	/// 只在本对象上参与规则解析，随对象生命周期结束而消失。执行器为单次操作追加的规则
	/// 走的就是这条通道（见 <c>ActuatorBase.WithRule</c>）。
	/// </remarks>
	public void AddInstanceRule(IRuleBase rule)
	{
		Check.EnsureNotNull(rule, nameof(rule));

		lock (_lockObject)
		{
			_instanceRules.Add(rule);
		}
	}

	/// <summary>
	/// 批量添加仅对当前业务对象实例生效的规则。
	/// </summary>
	/// <param name="rules">要添加的规则集合；为 <c>null</c> 时不执行任何操作。</param>
	public void AddInstanceRules(IEnumerable<IRuleBase> rules)
	{
		if (rules == null)
		{
			return;
		}

		foreach (var rule in rules)
		{
			AddInstanceRule(rule);
		}
	}

	/// <summary>
	/// 判断指定属性是否存在任何适用规则（类型级、实例级或本次操作级）。
	/// </summary>
	/// <param name="property">目标属性。</param>
	/// <returns>存在规则则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 供 <c>ObjectBase.PropertyHasChanged</c> 在属性 setter 上做快速短路：
	/// 没有规则的属性走普通变更通知即可，不必进入规则检查。因此本方法必须廉价。
	/// </remarks>
	internal bool HasRulesFor(IPropertyInfo property)
	{
		if (RuleManager.RulesFor(property).Count > 0)
		{
			return true;
		}

		return HasInstanceRulesFor(property);
	}

	/// <summary>
	/// 判断属性是否存在实例级或本次操作级规则（不含类型级共享规则）。
	/// </summary>
	/// <param name="property">属性。</param>
	/// <returns>存在则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 供 <c>ObjectBase.PropertyHasChanged</c> 在未创建实例级规则集合时短路：
	/// 先查类型级索引，再只在需要时进入本对象。
	/// </remarks>
	internal bool HasInstanceRulesFor(IPropertyInfo property)
	{
		if (_instanceRules.Count == 0 && Volatile.Read(ref _operationScopeCount) == 0)
		{
			return false;
		}

		lock (_lockObject)
		{
			if (_instanceRules.Any(rule => ReferenceEquals(rule.Property, property)))
			{
				return true;
			}

			foreach (var scope in _operationScopes)
			{
				if (scope.Rules.Any(rule => ReferenceEquals(rule.Property, property)))
				{
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>
	/// 解析本次对象级检查应运行的规则：类型级中<b>无属性绑定</b>者
	/// ∪ 全部实例级与操作级规则 − 排除集合，按优先级升序。
	/// </summary>
	/// <returns>规则列表。</returns>
	/// <remarks>
	/// <para>
	/// <b>对象级检查只跑「无属性绑定」的类型级规则</b>：属性级规则在<b>属性变更时</b>触发检查
	/// （见 <c>ObjectBase.PropertyHasChanged</c>），其违规已按属性归因落进
	/// <see cref="BrokenRules"/>，保存时通过 <see cref="IsValid"/> 生效，因此在这里再跑一遍没有意义。
	/// 更重要的是，在这里跑就等于校验「本次根本没改、甚至没装载」的属性——未装载的属性读出来是
	/// 默认值（空字符串），会凭空拦下正常保存。类型把属性检查推迟到保存时的情况由
	/// <c>ObjectBase.EnsureRulesAsync</c> 在调用本方法<b>之前</b>单独处理（只跑变更过的属性）。
	/// </para>
	/// <para>
	/// <b>实例级与操作级规则不按 <see cref="IRuleBase.Property"/> 过滤，全部纳入对象级检查</b>：
	/// 它们是调用方为「本次操作」显式附加的（执行器 <c>WithRule</c>），语义就是「这次必须满足」，
	/// 按属性过滤会让 <c>WithRule(new XxxRule(SomeProperty))</c> 静默不执行。
	/// 属性只用于结果归因（<see cref="BrokenRule.Property"/>）与变更通知。
	/// </para>
	/// <para>
	/// 共享集合取快照而非延迟枚举：它按类型进程级共享，边枚举边写入会抛「集合已被修改」。
	/// </para>
	/// </remarks>
	private IReadOnlyList<IRuleBase> ResolveObjectRules()
	{
		// 共享快照在 _lockObject 之外取：持 _lockObject 再去拿 RuleManager 的锁会与
		// InitializeRules（持 RuleManager 锁 → 使用方 AddRules → 可能进实例规则集合）形成锁序环。
		var shared = RuleManager.ObjectRules;
		var (supplemental, scopes) = SupplementalRules();

		if (supplemental.Length == 0 && scopes.Length == 0)
		{
			return shared;
		}

		return FilterByScope(shared.Concat(supplemental), scopes)
		       .OrderBy(rule => rule.Priority)
		       .ToList();
	}

	/// <summary>
	/// 解析指定属性的规则：类型级（走索引）与实例级/操作级中属性引用一致者，按优先级升序。
	/// </summary>
	/// <param name="property">目标属性。</param>
	/// <returns>规则列表。</returns>
	/// <remarks>
	/// 属性 setter 的热路径：类型级规则走 <see cref="RuleManager.RulesFor"/> 索引（无锁、无分配），
	/// 没有实例级/操作级规则时直接返回索引结果，连列表都不重建。
	/// </remarks>
	private IReadOnlyList<IRuleBase> ResolvePropertyRules(IPropertyInfo property)
	{
		var typed = RuleManager.RulesFor(property);
		var (supplemental, scopes) = SupplementalRules();

		if (supplemental.Length == 0 && scopes.Length == 0)
		{
			// 热路径：无实例级/操作级规则时直接返回类型级索引，不复制列表。
			return typed.Count == 0 ? Array.Empty<IRuleBase>() : typed;
		}

		var matched = supplemental.Where(rule => ReferenceEquals(rule.Property, property));

		return FilterByScope(typed.Concat(matched), scopes)
		       .OrderBy(rule => rule.Priority)
		       .ToList();
	}

	/// <summary>
	/// 取实例级与操作级规则的合并快照，以及本次操作范围（用于排除项）。
	/// </summary>
	/// <returns>合并后的规则数组与操作范围。</returns>
	private (IRuleBase[] Rules, RuleScope[] Scopes) SupplementalRules()
	{
		if (_instanceRules.Count == 0 && Volatile.Read(ref _operationScopeCount) == 0)
		{
			return (Array.Empty<IRuleBase>(), Array.Empty<RuleScope>());
		}

		lock (_lockObject)
		{
			if (_instanceRules.Count == 0 && _operationScopes.Count == 0)
			{
				return (Array.Empty<IRuleBase>(), Array.Empty<RuleScope>());
			}

			var scopes = _operationScopes.Count == 0 ? Array.Empty<RuleScope>() : [.. _operationScopes];
			var count = _instanceRules.Count;
			foreach (var scope in _operationScopes)
			{
				count += scope.Rules.Count;
			}

			var supplemental = new IRuleBase[count];
			_instanceRules.CopyTo(supplemental, 0);

			var offset = _instanceRules.Count;
			foreach (var scope in _operationScopes)
			{
				foreach (var rule in scope.Rules)
				{
					supplemental[offset++] = rule;
				}
			}

			return (supplemental, scopes);
		}
	}

	/// <summary>
	/// 剔除本次操作声明的排除项。
	/// </summary>
	/// <param name="rules">候选规则。</param>
	/// <param name="scopes">本次操作的规则范围；为空时不做剔除。</param>
	/// <returns>过滤后的规则。</returns>
	private static IEnumerable<IRuleBase> FilterByScope(IEnumerable<IRuleBase> rules, RuleScope[] scopes)
	{
		if (scopes.Length == 0)
		{
			return rules;
		}

		return rules.Where(rule => !scopes.Any(scope => scope.IsExcluded(rule)));
	}

	/// <summary>
	/// 向业务规则管理器添加规则。
	/// </summary>
	/// <typeparam name="TRule">规则类型。</typeparam>
	public void AddRule<TRule>()
		where TRule : class, IRuleBase, new()
	{
		AddRule(new TRule());
	}

	/// <summary>
	/// 向业务规则管理器添加规则。
	/// </summary>
	/// <param name="provider">用于解析规则实例的服务提供程序。</param>
	/// <typeparam name="TRule">规则类型。</typeparam>
	public void AddRule<TRule>(IServiceProvider provider)
		where TRule : class, IRuleBase
	{
		var rule = ActivatorUtilities.GetServiceOrCreateInstance<TRule>(provider);
		AddRule(rule);
	}

	#region Rule check

	/// <summary>
	/// 检查当前对象的规则。
	/// </summary>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <returns>受规则影响且发生变化的属性名称列表。</returns>
	/// <remarks>
	/// <para>
	/// <b>同步契约的固有阻塞</b>：本方法与 <see cref="CheckRules(IPropertyInfo)"/> 一样运行在
	/// 调用线程上——这是同步 API 的既定代价，不是缺陷（O-4 方案 A 的 fail-fast 曾试过
	/// 把「未同步完成的异步规则」变成异常，因破坏既有契约被否决并回滚）。
	/// </para>
	/// <para>
	/// <b>异步调用方请改走 <see cref="CheckObjectRulesAsync"/>（O-4 方案 B）</b>：
	/// 它是本方法的异步对等物（同一解析、同一裁决、同一通知），只是 <c>await</c> 而不是
	/// <c>Task.WaitAll</c>——不会阻塞线程池线程，也就不存在「并发下线程池饥饿」。
	/// 生产代码中的保存（<c>EditableObject.SaveAsync</c>）、命令执行（<c>ObjectRuleGuard</c>）
	/// 与 <c>ObjectBase.ValidateAsync</c> 全部已经走异步入口；
	/// 本方法保留给纯同步宿主（WPF/WinForms 命令处理器等）使用。
	/// </para>
	/// </remarks>
	public List<string> CheckObjectRules(bool cascade)
	{
		if (IsRuleCheckingSuspended)
		{
			return new List<string>();
		}

		// 与 CheckRules 同理：有同步上下文时必须整轮换到线程池启动，否则阻塞线程与
		// 需要回到该线程才能完成的规则互等而死锁。
		if (SynchronizationContext.Current != null)
		{
			return Task.Run(() => RunObjectRules(cascade)).GetAwaiter().GetResult();
		}

		return RunObjectRules(cascade);
	}

	/// <summary>
	/// 同步执行对象级规则并收集结果。
	/// </summary>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <returns>受规则影响且发生变化的属性名称列表。</returns>
	private List<string> RunObjectRules(bool cascade)
	{
		var currentRunningState = HasRunningRules;
		var rules = PrepareObjectRules();
		var (properties, tasks) = RunRules(rules, cascade, out var executed);
		if (tasks.Count > 0)
		{
			Task.WaitAll([.. tasks]);
		}

		if (executed > 0 && !currentRunningState)
		{
			_target.AllRulesComplete();
		}

		return properties.Distinct().ToList();
	}

	/// <summary>
	/// 异步检查当前对象的规则。
	/// </summary>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>受规则影响且发生变化的属性名称列表。</returns>
	public async Task<List<string>> CheckObjectRulesAsync(bool cascade, CancellationToken cancellationToken = default)
	{
		if (IsRuleCheckingSuspended)
		{
			return new List<string>();
		}

		var currentRunningState = HasRunningRules;
		var rules = PrepareObjectRules();
		var (properties, tasks) = RunRules(rules, cascade, out var executed, cancellationToken);
		if (tasks.Count > 0)
		{
			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		if (executed > 0 && !currentRunningState)
		{
			_target.AllRulesComplete();
		}

		return properties.Distinct().ToList();
	}

	/// <summary>
	/// 运行对象级规则，存在 Error 级违规时抛出 <see cref="ValidationException"/>。
	/// </summary>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <param name="message">验证异常的说明。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// <para>
	/// 保存与命令执行两条路径的<b>唯一</b>裁决实现（分别由 <c>EditableObject.SaveAsync</c> 与
	/// <c>ObjectRuleGuard</c> 调用），避免两处各写一遍必然漂移。
	/// </para>
	/// <para>
	/// <b>挂起时不给结论、也不抛出</b>：此时 <see cref="BrokenRules"/> 反映的是上一次检查，
	/// 据此放行或拦截都是错的。
	/// </para>
	/// </remarks>
	/// <exception cref="ValidationException">存在 Error 级违规时抛出。</exception>
	internal async Task EnsureObjectRulesAsync(bool cascade, string message, CancellationToken cancellationToken = default)
	{
		if (IsRuleCheckingSuspended)
		{
			return;
		}

		// 没有对象级规则（也没有实例级/操作级规则）时不需要启动一轮规则执行：
		// 属性级违规已经在变更时落进 BrokenRules，直接按当前结论裁决即可。
		if (RuleManager.ObjectRules.Count == 0 && !HasSupplementalRules)
		{
			if (IsValid)
			{
				return;
			}

			var existing = BrokenRules.Select(t => new ValidationResult(t.Property, t.Description));
			throw new ValidationException(message, existing);
		}

		await CheckObjectRulesAsync(cascade, cancellationToken);

		if (IsValid)
		{
			return;
		}

		var errors = BrokenRules.Select(t => new ValidationResult(t.Property, t.Description));
		throw new ValidationException(message, errors);
	}

	/// <summary>
	/// 解析对象级规则，并清空上一轮由这些规则产生的违规记录。
	/// </summary>
	/// <returns>本次要运行的规则列表。</returns>
	/// <remarks>
	/// 清理必须<b>定向</b>到本轮即将运行的规则所涉及的属性：实例级规则可以绑定属性，
	/// 若只清 <c>null</c>，同一对象上的重复检查会逐轮累积重复条目。
	/// </remarks>
	private IReadOnlyList<IRuleBase> PrepareObjectRules()
	{
		var rules = ResolveObjectRules();

		// 没有对象级规则时不需要清空/创建违规集合：集合里不会有过期条目。
		if (rules.Count == 0)
		{
			return rules;
		}

		BrokenRules.ClearRules(null);

		// 比较口径必须与 BrokenRuleCollection.ClearRules(string) 的序数相等保持一致，
		// 否则仅大小写不同的两个属性只会清理其中一个，陈旧条目跨轮累积导致 IsValid 失真。
		foreach (var property in rules.Select(rule => rule.Property)
		                              .Where(property => property != null)
		                              .DistinctBy(property => property.Name))
		{
			BrokenRules.ClearRules(property);
		}

		return rules;
	}

	/// <summary>
	/// 检查指定属性的规则。
	/// </summary>
	/// <param name="property">要检查规则的属性。</param>
	/// <returns>受规则影响且发生变化的属性名称列表。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="property"/> 为 <c>null</c> 时抛出。</exception>
	/// <remarks>
	/// <para>
	/// <b>setter 热路径的同步契约</b>：本方法由 <c>ObjectBase.CheckPropertyRules</c> 在属性变更时调用，
	/// 阻塞调用线程是同步 API 的既定代价（也是 <c>CheckRuleOnPropertyChanged</c> 约束的来源：
	/// 属性级规则应当是同步即可完成的纯校验）。
	/// </para>
	/// <para>
	/// <b>含 I/O 的属性级校验请改走 <see cref="CheckRulesAsync(IPropertyInfo, CancellationToken)"/>（O-4 方案 B）</b>：
	/// 那条路径把属性检查推迟到保存时（<c>CheckRuleOnPropertyChanged =&gt; false</c> 的类型），
	/// 在异步流程里 <c>await</c> 而不是 <c>Task.WaitAll</c>——不阻塞线程池线程。
	/// </para>
	/// </remarks>
	public List<string> CheckRules(IPropertyInfo property)
	{
		if (property == null)
		{
			throw new ArgumentNullException(nameof(property));
		}

		if (IsRuleCheckingSuspended)
		{
			return new List<string> { property.Name };
		}

		var rules = ResolvePropertyRules(property);

		// 该属性没有可运行的规则（无规则，或规则全被本次操作排除）：不起任务、不做清理。
		// 属性 setter 是热路径，绝大多数属性没有规则，这里必须能直接返回。
		//
		// 返回本属性名而不是空集合，是为了与下面的「挂起」分支口径一致：两种情况都表示
		// 「本次没有规则检查来驱动变更通知」，调用方（ObjectBase.CheckPropertyRules）
		// 据此补发一次普通通知——否则被排除的属性会在 setter 上悄悄丢掉 PropertyChanged。
		if (rules.Count == 0)
		{
			return [property.Name];
		}

		if (SynchronizationContext.Current != null)
		{
			// 必须在**启动规则之前**就换到线程池：规则一旦在调用线程上启动，
			// 它的异步续体就已被排回该线程，之后再怎么等都会互等。
			return Task.Run(() => RunPropertyRules(property, rules)).GetAwaiter().GetResult();
		}

		return RunPropertyRules(property, rules);
	}

	/// <summary>
	/// 异步检查指定属性的规则（不在 setter 上调用，无变更通知职责）。
	/// </summary>
	/// <param name="property">目标属性。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// 供「推迟到保存时检查」的路径使用：那时已经在异步流程里，不必像 <see cref="CheckRules"/>
	/// 那样同步阻塞调用线程，属性级规则也就可以放心 await（I/O 校验）。
	/// </remarks>
	internal async Task CheckRulesAsync(IPropertyInfo property, CancellationToken cancellationToken = default)
	{
		if (property == null)
		{
			throw new ArgumentNullException(nameof(property));
		}

		if (IsRuleCheckingSuspended)
		{
			return;
		}

		var rules = ResolvePropertyRules(property);

		if (rules.Count == 0)
		{
			return;
		}

		BrokenRules.ClearRules(property);

		var (_, tasks) = RunRules(rules, true, out _, cancellationToken);

		await Task.WhenAll(tasks);
	}

	/// <summary>
	/// 依次异步检查给定属性的规则。
	/// </summary>
	/// <param name="properties">要检查的属性；为 <c>null</c> 或空时不执行任何操作。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// 逐个属性串行执行（而不是一次性并发），以保证违规记录的顺序与属性顺序一致——
	/// 这些记录会直接成为表单上的字段级错误列表。
	/// </remarks>
	internal async Task CheckRulesAsync(IEnumerable<IPropertyInfo> properties, CancellationToken cancellationToken = default)
	{
		if (properties == null)
		{
			return;
		}

		foreach (var property in properties)
		{
			await CheckRulesAsync(property, cancellationToken);
		}
	}

	/// <summary>
	/// 同步执行指定属性的规则并收集结果。
	/// </summary>
	/// <param name="property">目标属性。</param>
	/// <param name="rules">已解析的规则。</param>
	/// <returns>受规则影响且发生变化的属性名称列表。</returns>
	/// <remarks>
	/// 属性 setter 上会<b>阻塞</b>调用线程——这是同步 API 的固有代价，也是
	/// <c>ObjectBase.CheckRuleOnPropertyChanged</c> 约束的来源：属性级规则应当是
	/// 同步即可完成的纯校验；需要 I/O 的校验请放对象级规则（保存走异步路径）。
	/// </remarks>
	private List<string> RunPropertyRules(IPropertyInfo property, IReadOnlyList<IRuleBase> rules)
	{
		BrokenRules.ClearRules(property);

		var (properties, tasks) = RunRules(rules, true, out _);

		if (tasks.Count > 0)
		{
			Task.WaitAll([.. tasks]);
		}

		// CheckRules 是 public：调用方可能修改返回列表，不能返回共享实例。
		return properties.Count == 0 ? new List<string>(0) : properties.Distinct().ToList();
	}

	/// <summary>
	/// 为指定属性执行所有规则检查逻辑。
	/// </summary>
	/// <param name="property">
	/// 要执行属性规则检查的属性。
	/// </param>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>受影响的属性列表和规则任务列表。</returns>
	private Tuple<List<string>, List<Task>> CheckRulesForProperty(IPropertyInfo property, bool cascade, CancellationToken cancellationToken = default)
	{
		var rules = ResolvePropertyRules(property);

		BrokenRules.ClearRules(property);

		return RunRules(rules, cascade, out _, cancellationToken);
	}

	/// <summary>
	/// 运行规则检查。
	/// </summary>
	/// <param name="rules">要运行的规则集合。</param>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <param name="executedCount">实际启动的规则数量（已按 <see cref="ExecuteOnStateAttribute"/> 过滤）。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>受影响的属性列表和规则任务列表。</returns>
	private Tuple<List<string>, List<Task>> RunRules(IEnumerable<IRuleBase> rules, bool cascade, out int executedCount, CancellationToken cancellationToken = default)
	{
		List<string> affectProperties = null;
		List<Task> tasks = null;
		var editableObject = Target as IEditableObject;
		executedCount = 0;

		foreach (var rule in rules)
		{
			if (editableObject != null)
			{
				var states = GetRuleStates(rule.GetType());

				// 空 States 视为「不限制」而不是「全都不匹配」：后者会让一个没填参数的特性
				// 静默把规则彻底关掉（且毫无提示），而规则该跑起来才是安全的方向。
				if (states.Length > 0 && Array.IndexOf(states, editableObject.State) < 0)
				{
					continue;
				}
			}

			executedCount++;

			var context = new RuleContext(_ruleCompleted)
			{
				Target = Target,
				Rule = rule,
				PropertyName = rule.Property?.Name
			};

			if (cascade && rule.RelatedProperties.Count > 0)
			{
				lock (_lockObject)
				{
					foreach (var property in rule.RelatedProperties)
					{
						var (properties, cascadeTasks) = CheckRulesForProperty(property, false, cancellationToken);
						if (properties.Count > 0)
						{
							(affectProperties ??= []).AddRange(properties);
						}

						if (cascadeTasks.Count > 0)
						{
							(tasks ??= []).AddRange(cascadeTasks);
						}
					}
				}
			}

			lock (_lockObject)
			{
				RunningRules.Add(rule);
			}

			var task = RunRule(rule, context, cancellationToken);
			if (!task.IsCompletedSuccessfully)
			{
				(tasks ??= []).Add(task);
			}
		}

		return Tuple.Create(affectProperties ?? EmptyAffectedProperties, tasks ?? EmptyTasks);
	}

	/// <summary>
	/// 运行单条规则；同步完成的规则走无状态机快路径。
	/// </summary>
	/// <param name="rule">规则。</param>
	/// <param name="context">上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>规则任务。</returns>
	private static Task RunRule(IRuleBase rule, IRuleContext context, CancellationToken cancellationToken)
	{
		var started = Stopwatch.GetTimestamp();
		Task task;
		try
		{
			task = rule.ExecuteAsync(context, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			OsbaDiagnostics.RecordRule(started, rule.Name, false);
			context.Complete();
			throw;
		}
		catch (Exception ex)
		{
			context.AddErrorResult(Describe(rule, ex));
			OsbaDiagnostics.RecordRule(started, rule.Name, false);
			context.Complete();
			return Task.CompletedTask;
		}

		if (task == null || task.IsCompletedSuccessfully)
		{
			var succeeded = !context.Results.Any(result => result.Severity == RuleSeverity.Error);
			OsbaDiagnostics.RecordRule(started, rule.Name, succeeded);
			context.Complete();
			return Task.CompletedTask;
		}

		return AwaitRuleAsync(rule, task, context, started);
	}

	/// <summary>
	/// 等待未同步完成的规则，并保持“取消原样上抛、其他异常转错误结果”的契约。
	/// </summary>
	/// <param name="rule">规则。</param>
	/// <param name="task">规则任务。</param>
	/// <param name="context">上下文。</param>
	/// <param name="started">起始时间戳。</param>
	/// <returns>规则任务。</returns>
	private static async Task AwaitRuleAsync(IRuleBase rule, Task task, IRuleContext context, long started)
	{
		var succeeded = false;
		try
		{
			await task.ConfigureAwait(false);
			succeeded = !context.Results.Any(result => result.Severity == RuleSeverity.Error);
		}
		catch (OperationCanceledException)
		{
			// 取消必须原样传播：若把它收敛成一条错误结果，
			// 调用方看到的就是「校验不通过」，而实际是「操作已被取消」，
			// 两者的处置方式完全不同（前者重填数据，后者直接返回）。
			throw;
		}
		catch (Exception ex)
		{
			// 规则自身抛出的异常仍然收敛为错误结果（规则检查不该让整个保存流程直接崩掉），
			// 但描述里必须带上异常类型与内部异常，否则 NRE 之类的编程错误会被伪装成
			// 一句孤零零的业务提示，线上根本无法定位。
			context.AddErrorResult(Describe(rule, ex));
		}
		finally
		{
			OsbaDiagnostics.RecordRule(started, rule.Name, succeeded);
			context.Complete();
		}
	}

	/// <summary>
	/// 规则完成回调：写入违规、维护运行中规则集、发出属性变更通知。
	/// </summary>
	/// <param name="ruleContext">规则上下文。</param>
	private void OnRuleCompleted(IRuleContext ruleContext)
	{
		List<IPropertyInfo> notify = null;

		lock (_lockObject)
		{
			var results = ruleContext.Results;
			if (results.Count > 0)
			{
				BrokenRules.Add(results, ruleContext.Rule.Property?.Name);
			}

			RunningRules.Remove(ruleContext.Rule);

			var property = ruleContext.Rule.Property;
			if (property != null && IsNotRunning(property))
			{
				(notify ??= []).Add(property);
			}

			foreach (var related in ruleContext.Rule.RelatedProperties)
			{
				if (IsNotRunning(related))
				{
					(notify ??= []).Add(related);
				}
			}
		}

		// RuleCheckComplete 是用户可重写的成员（会走 PropertyChanged → 用户事件处理器），
		// 把任意用户代码放进 _lockObject 的临界区，等于让外部代码握着本锁去等它自己的锁：
		// 一旦另一条线程反过来先拿外层锁再来要 _lockObject，就是互相咬死。
		// 判定已经原子完成，这里只负责把结果发出去。
		if (notify == null)
		{
			return;
		}

		foreach (var property in notify)
		{
			_target.RuleCheckComplete(property);
		}
	}

	/// <summary>
	/// 判断属性是否仍有规则在运行；调用方必须持有 <see cref="_lockObject"/>。
	/// </summary>
	/// <param name="property">属性。</param>
	/// <returns>没有规则在运行则返回 <see langword="true"/>。</returns>
	private bool IsNotRunning(IPropertyInfo property)
	{
		foreach (var running in RunningRules)
		{
			if (ReferenceEquals(running.Property, property))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// 取规则类型上声明的执行状态；无特性或特性为空时返回空数组（表示不限制）。
	/// </summary>
	/// <param name="ruleType">规则类型。</param>
	/// <returns>状态数组。</returns>
	private static ObjectEditState[] GetRuleStates(Type ruleType)
	{
		return _ruleStates.GetOrAdd(ruleType, static type =>
			type.GetCustomAttribute<ExecuteOnStateAttribute>()?.States?.ToArray() ?? Array.Empty<ObjectEditState>());
	}

	/// <summary>
	/// 空的受影响属性列表（只读使用，不可修改）。
	/// </summary>
	private static readonly List<string> EmptyAffectedProperties = [];

	/// <summary>
	/// 空的任务列表（只读使用，不可修改）。
	/// </summary>
	private static readonly List<Task> EmptyTasks = [];

	/// <summary>
	/// 把规则抛出的异常渲染成错误结果描述，保留异常类型与内部异常信息。
	/// </summary>
	/// <param name="rule">抛出异常的规则。</param>
	/// <param name="exception">规则抛出的异常。</param>
	/// <returns>错误描述。</returns>
	private static string Describe(IRuleBase rule, Exception exception)
	{
		var description = $"{rule.Name}: [{exception.GetType().Name}] {exception.Message}";

		var inner = exception.InnerException;
		while (inner != null)
		{
			description += $" -> [{inner.GetType().Name}] {inner.Message}";
			inner = inner.InnerException;
		}

		return description;
	}

	#endregion

	#region DataAnnotations

	/// <summary>
	/// 向业务规则管理器添加数据注解规则。
	/// </summary>
	public void AddDataAnnotations()
	{
		var registeredProperties = ((IOsbaObject)_target).FieldManager.GetRegisteredProperties();

		if (registeredProperties == null || registeredProperties.Count == 0)
		{
			return;
		}

		var properties = _target.GetType().GetRuntimeProperties();

		foreach (var property in properties)
		{
			var registeredProperty = registeredProperties.FirstOrDefault(t => t.Name == property.Name);
			if (registeredProperty == null)
			{
				continue;
			}

			var attributes = property.GetCustomAttributes<ValidationAttribute>(true);
			foreach (var attribute in attributes)
			{
				AddRule(new DataAnnotationRule(registeredProperty, attribute));
			}
		}
	}

	#endregion
}