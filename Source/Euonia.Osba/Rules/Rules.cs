using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
// 本文件已导入 System.ComponentModel.DataAnnotations（AddDataAnnotations 需要 ValidationAttribute），
// 该命名空间下同样有 ValidationResult / ValidationException，但签名不同且不含属性名。
// 这里要的是框架自己的验证类型，故显式取别名，避免误解析。
using ValidationException = Nerosoft.Euonia.Validation.ValidationException;
using ValidationResult = Nerosoft.Euonia.Validation.ValidationResult;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="IRules"/> 接口的实现。
/// </summary>
public class Rules : IRules
{
	private readonly object _lockObject = new();

	internal Rules(IHasRuleCheck @object)
	{
		_target = @object;
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
			if (field == null && Target != null)
			{
				field = RuleManager.GetRules(Target.GetType());
			}

			return field;
		}
	}

	/// <summary>
	/// 获取一个值，指示当前是否存在违规规则，存在则意味着对象无效。
	/// </summary>
	public bool IsValid => BrokenRules?.ErrorCount == 0;

	internal BrokenRuleCollection BrokenRules { get; } = new();

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
	/// 本次操作挂上的规则范围（由执行器在操作开始时挂载、结束时卸下）。
	/// </summary>
	private RuleScope _operationScope;

	/// <summary>
	/// 获取一个值，指示规则检查当前是否被挂起——即「本次不给出结论」。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 两个来源：调用方显式 <see cref="SuppressRuleChecking"/>（<c>SuspendRuleChecking</c>），
	/// 或本次操作声明跳过检查（<c>ActuatorBase.WithoutRuleChecks</c>）。两者语义一致，故合并成一个判定入口。
	/// </para>
	/// <para>
	/// <b>挂起不等于「对象有效」</b>：挂起期间既不跑规则、也不读陈旧的 <see cref="BrokenRules"/>——
	/// 后者反映的是上一次检查，据此放行或拦截都是错的（这正是 <c>SuspendRuleChecking</c> 后
	/// 保存会按旧结论抛错的原因）。
	/// </para>
	/// </remarks>
	internal bool IsRuleCheckingSuspended => SuppressRuleChecking || (_operationScope?.SkipChecks ?? false);

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
		_operationScope = scope;
	}

	/// <summary>
	/// 卸下本次操作的规则范围。
	/// </summary>
	internal void DetachOperationRules()
	{
		_operationScope = null;
	}

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
	/// 判断指定类型的规则是否已在规则解析范围内（类型级、实例级或本次操作级）。
	/// </summary>
	/// <param name="ruleType">规则类型，按精确类型匹配。</param>
	/// <returns>已存在则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 供框架做「幂等注入」用（避免同一规则被注册两次、错误重复）。
	/// 只判类型不判实例：同名不同参数的规则（如两条消息不同的 <see cref="CommonRule.Required"/>）
	/// 是合法用法，本方法不用于去重它们。
	/// </remarks>
	internal bool ContainsRule(Type ruleType)
	{
		// 先取共享快照再进本对象的锁，避免与 InitializeRules 的锁序相互等待
		var shared = RuleManager.Snapshot();

		lock (_lockObject)
		{
			return shared.Any(rule => rule.GetType() == ruleType)
			       || _instanceRules.Any(rule => rule.GetType() == ruleType)
			       || (_operationScope != null && _operationScope.Rules.Any(rule => rule.GetType() == ruleType));
		}
	}

	/// <summary>
	/// 判断指定属性是否存在任何适用规则（类型级、实例级或本次操作级）。
	/// </summary>
	/// <param name="property">目标属性。</param>
	/// <returns>存在规则则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 供 <c>BusinessObject.PropertyHasChanged</c> 在属性 setter 上做快速短路：
	/// 没有规则的属性走普通变更通知即可，不必进入规则检查。因此本方法必须廉价。
	/// </remarks>
	internal bool HasRulesFor(IPropertyInfo property)
	{
		if (RuleManager.RulesFor(property).Count > 0)
		{
			return true;
		}

		lock (_lockObject)
		{
			return _instanceRules.Any(rule => ReferenceEquals(rule.Property, property))
			       || (_operationScope != null && _operationScope.Rules.Any(rule => ReferenceEquals(rule.Property, property)));
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
	/// （见 <c>BusinessObject.PropertyHasChanged</c>），其违规已按属性归因落进
	/// <see cref="BrokenRules"/>，保存时通过 <see cref="IsValid"/> 生效，因此在这里再跑一遍没有意义。
	/// 更重要的是，在这里跑就等于校验「本次根本没改、甚至没装载」的属性——未装载的属性读出来是
	/// 默认值（空字符串），会凭空拦下正常保存。类型把属性检查推迟到保存时的情况由
	/// <c>BusinessObject.EnsureRulesAsync</c> 在调用本方法<b>之前</b>单独处理（只跑变更过的属性）。
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
	private List<IRuleBase> ResolveObjectRules()
	{
		// 共享快照在 _lockObject 之外取：持 _lockObject 再去拿 RuleManager 的锁会与
		// InitializeRules（持 RuleManager 锁 → 使用方 AddRules → 可能进实例规则集合）形成锁序环。
		var shared = RuleManager.Snapshot().Where(rule => rule.Property == null);
		var (supplemental, scope) = SupplementalRules();

		return FilterByScope(shared.Concat(supplemental), scope)
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
	private List<IRuleBase> ResolvePropertyRules(IPropertyInfo property)
	{
		var typed = RuleManager.RulesFor(property);
		var (supplemental, scope) = SupplementalRules();

		if (supplemental.Length == 0 && scope == null)
		{
			return typed.Count == 0 ? [] : [.. typed];
		}

		var matched = supplemental.Where(rule => ReferenceEquals(rule.Property, property));

		return FilterByScope(typed.Concat(matched), scope)
		       .OrderBy(rule => rule.Priority)
		       .ToList();
	}

	/// <summary>
	/// 取实例级与操作级规则的合并快照，以及本次操作范围（用于排除项）。
	/// </summary>
	/// <returns>合并后的规则数组与操作范围。</returns>
	private (IRuleBase[] Rules, RuleScope Scope) SupplementalRules()
	{
		IRuleBase[] instance;
		IRuleBase[] operationRules;
		RuleScope scope;

		lock (_lockObject)
		{
			instance = [.. _instanceRules];
			scope = _operationScope;
			operationRules = scope == null ? [] : [.. scope.Rules];
		}

		// 实例级与操作级规则一视同仁：两者都「只对当前对象生效」，区别仅在生命周期
		var supplemental = new IRuleBase[instance.Length + operationRules.Length];
		instance.CopyTo(supplemental, 0);
		operationRules.CopyTo(supplemental, instance.Length);

		return (supplemental, scope);
	}

	/// <summary>
	/// 剔除本次操作声明的排除项。
	/// </summary>
	/// <param name="rules">候选规则。</param>
	/// <param name="scope">本次操作的规则范围；为 <c>null</c> 时不做剔除。</param>
	/// <returns>过滤后的规则。</returns>
	private static IEnumerable<IRuleBase> FilterByScope(IEnumerable<IRuleBase> rules, RuleScope scope)
	{
		return scope == null ? rules : rules.Where(rule => !scope.IsExcluded(rule));
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
		var (properties, tasks) = RunRules(rules, cascade);
		Task.WaitAll([.. tasks]);
		if (tasks.Count > 0 && !currentRunningState)
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
		var (properties, tasks) = RunRules(rules, cascade, cancellationToken);
		await Task.WhenAll(tasks);
		if (tasks.Count > 0 && !currentRunningState)
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
	private List<IRuleBase> PrepareObjectRules()
	{
		var rules = ResolveObjectRules();

		BrokenRules.ClearRules(null);

		foreach (var property in rules.Select(rule => rule.Property)
		                              .Where(property => property != null)
		                              .DistinctBy(property => property.Name, StringComparer.OrdinalIgnoreCase))
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
		// 「本次没有规则检查来驱动变更通知」，调用方（BusinessObject.CheckPropertyRules）
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

		var (_, tasks) = RunRules(rules, true, cancellationToken);

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
	/// <c>BusinessObject.CheckRuleOnPropertyChanged</c> 约束的来源：属性级规则应当是
	/// 同步即可完成的纯校验；需要 I/O 的校验请放对象级规则（保存走异步路径）。
	/// </remarks>
	private List<string> RunPropertyRules(IPropertyInfo property, List<IRuleBase> rules)
	{
		BrokenRules.ClearRules(property);

		var (properties, tasks) = RunRules(rules, true);

		if (tasks.Count > 0)
		{
			Task.WaitAll([.. tasks]);
		}

		return properties.Distinct().ToList();
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

		return RunRules(rules, cascade, cancellationToken);
	}

	/// <summary>
	/// 运行规则检查。
	/// </summary>
	/// <param name="rules">要运行的规则集合。</param>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>受影响的属性列表和规则任务列表。</returns>
	private Tuple<List<string>, List<Task>> RunRules(IEnumerable<IRuleBase> rules, bool cascade, CancellationToken cancellationToken = default)
	{
		var affectProperties = new List<string>();
		var tasks = new List<Task>();
		foreach (var rule in rules)
		{
			if (Target is IEditableObject editableObject)
			{
				var attribute = rule.GetType().GetCustomAttribute<ExecuteOnStateAttribute>();

				// 空 States 视为「不限制」而不是「全都不匹配」：后者会让一个没填参数的特性
				// 静默把规则彻底关掉（且毫无提示），而规则该跑起来才是安全的方向。
				if (attribute?.States?.Any() == true && !attribute.States.Contains(editableObject.State))
				{
					continue;
				}
			}

			var context = new RuleContext(ruleContext =>
			{
				lock (_lockObject)
				{
					BrokenRules.Add(ruleContext.Results, ruleContext.Rule.Property?.Name);

					RunningRules.Remove(ruleContext.Rule);

					var properties = Enumerable.Empty<IPropertyInfo>();

					if (ruleContext.Rule.Property != null)
					{
						properties = properties.Append(ruleContext.Rule.Property);
					}

					properties = properties.Concat(ruleContext.Rule.RelatedProperties);

					foreach (var property in properties)
					{
						if (RunningRules.All(r => r.Property != property))
						{
							_target.RuleCheckComplete(property);
						}
					}
				}
			})
			{
				Target = Target,
				Rule = rule,
				PropertyName = rule.Property?.Name
			};

			if (cascade)
			{
				lock (_lockObject)
				{
					foreach (var property in rule.RelatedProperties)
					{
						var (properties, cascadeTasks) = CheckRulesForProperty(property, false, cancellationToken);
						affectProperties.AddRange(properties);
						tasks.AddRange(cascadeTasks);
					}
				}
			}

			lock (_lockObject)
			{
				RunningRules.Add(rule);
			}

			tasks.Add(RunAsync(rule, context, cancellationToken));
		}

		return Tuple.Create(affectProperties, tasks);
	}

	/// <summary>
	/// 运行异步规则检查任务。
	/// </summary>
	/// <param name="rule">要执行的规则。</param>
	/// <param name="context">规则上下文。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	private static async Task RunAsync(IRuleBase rule, IRuleContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			await rule.ExecuteAsync(context, cancellationToken);
		}
		catch (Exception ex)
		{
			context.AddErrorResult($"{rule.Name}: {ex.Message}");
		}
		finally
		{
			context.Complete();
		}
	}

	#endregion

	#region DataAnnotations

	/// <summary>
	/// 向业务规则管理器添加数据注解规则。
	/// </summary>
	public void AddDataAnnotations()
	{
		var registeredProperties = ((IBusinessObject)_target).FieldManager.GetRegisteredProperties();

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