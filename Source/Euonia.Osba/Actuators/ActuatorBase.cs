using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 执行器抽象基类，封装了业务对象的获取、处理、终结（保存或执行）的通用流程。
/// </summary>
/// <typeparam name="TTarget">业务对象的具体类型，必须继承自 <see cref="BusinessObject{T}"/>。</typeparam>
/// <remarks>
/// <para>
/// 派生类通过 <see cref="ActuatorBase{TTarget}(ActuatorBuilder{TTarget}, Func{Task{TTarget}})"/> 构造函数接收
/// 构建器配置与对象工厂委托，并通过 <see cref="Handle(System.Func{TTarget,System.Threading.Tasks.Task})"/> 或
/// <see cref="Handle(Action{TTarget})"/> 注册处理逻辑；调用 <see cref="ExecuteAsync(CancellationToken)"/> 触发完整流程。
/// 终步骤 <see cref="FinalizeAsync(TTarget, CancellationToken)"/> 由派生类实现：
/// 可编辑对象（<see cref="EditableActuator{TTarget}"/>）执行保存，命令对象（<see cref="ExecuteActuator{TTarget}"/>）执行命令体。
/// </para>
/// <para>
/// 规则可由调用方按操作指定（<see cref="WithRule(IRuleBase)"/> 等）。附加的规则只挂在本次操作取到的
/// 对象上，不写入按类型共享的规则管理器；终结步骤（保存 / 命令执行）才是规则唯一的裁决点，
/// 因此规则看到的必然是 <see cref="Handle(Action{TTarget})"/> 处理之后的对象状态。
/// </para>
/// </remarks>
public abstract class ActuatorBase<TTarget>
	where TTarget : BusinessObject<TTarget>
{
	/// <summary>
	/// 初始化执行器基类，保存构建器配置和对象工厂委托。
	/// </summary>
	/// <param name="builder">包含执行管道配置的构建器实例。</param>
	/// <param name="factory">用于异步获取或创建目标对象的工厂委托。</param>
	protected ActuatorBase(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
	{
		Builder = builder;
		Factory = factory;
	}

	/// <summary>
	/// 获取执行器的构建器配置，其中包含供 <see cref="ExecuteAsync(CancellationToken)"/> 使用的执行管道。
	/// </summary>
	protected ActuatorBuilder<TTarget> Builder { get; }

	/// <summary>
	/// 获取用于异步获取或创建目标对象的工厂委托。
	/// </summary>
	protected Func<Task<TTarget>> Factory { get; }

	/// <summary>
	/// 本次操作的规则范围；未使用规则 API 时为 <see langword="null"/>。
	/// </summary>
	/// <remarks>
	/// 惰性创建：既有调用方不关心规则时零开销、也不改变任何行为。
	/// </remarks>
	private RuleScope _ruleScope;

	/// <summary>
	/// 获取本次操作的规则范围，按需创建。
	/// </summary>
	private RuleScope RuleScope => _ruleScope ??= new RuleScope();

	/// <summary>
	/// 在主要处理逻辑完成后、终结步骤前执行的后续处理。派生类可重写以添加额外操作。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步操作的 <see cref="Task"/>。</returns>
	protected virtual Task ContinueHandleAsync(TTarget target, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 执行终结步骤：对已处理的目标对象执行最终操作（可编辑对象保存、命令对象执行）。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步终结操作的任务，包含处理完成后的目标对象。</returns>
	protected abstract Task<TTarget> FinalizeAsync(TTarget target, CancellationToken cancellationToken);

	/// <summary>
	/// 注册对目标对象的异步处理逻辑，返回当前执行器以支持链式调用。
	/// </summary>
	/// <param name="action">对目标对象执行的异步操作。</param>
	/// <returns>当前 <see cref="ActuatorBase{TTarget}"/> 实例，用于链式调用。</returns>
	/// <remarks>
	/// 注册的处理逻辑将在 <see cref="ExecuteAsync(CancellationToken)"/> 执行流程中、终结步骤之前被调用。
	/// </remarks>
	public ActuatorBase<TTarget> Handle(Func<TTarget, Task> action)
	{
		Builder.Pipeline.Use(async (target, next) =>
		{
			await action(target);
			return await next(target);
		});
		return this;
	}

	/// <summary>
	/// 注册对目标对象的同步处理逻辑，返回当前执行器以支持链式调用。
	/// </summary>
	/// <param name="action">对目标对象执行的同步操作。</param>
	/// <returns>当前 <see cref="ActuatorBase{TTarget}"/> 实例，用于链式调用。</returns>
	/// <remarks>
	/// 同步逻辑会被包装为异步委托，并在 <see cref="ExecuteAsync(CancellationToken)"/> 执行流程中、终结步骤之前被调用。
	/// </remarks>
	public ActuatorBase<TTarget> Handle(Action<TTarget> action)
	{
		Builder.Pipeline.Use(async (target, next) =>
		{
			action(target);
			return await next(target);
		});
		return this;
	}

	#region Rule specification

	/// <summary>
	/// 为本次操作附加一条规则。
	/// </summary>
	/// <param name="rule">要附加的规则。</param>
	/// <returns>当前执行器，用于链式调用。</returns>
	/// <remarks>
	/// 规则写入<b>目标对象实例</b>的规则集合，不进入按类型进程级共享的规则管理器——
	/// 因此并发请求之间不会互相看到对方的规则，操作结束后也随对象一并消失。
	/// </remarks>
	public ActuatorBase<TTarget> WithRule(IRuleBase rule)
	{
		Check.EnsureNotNull(rule, nameof(rule));

		RuleScope.Add(rule);
		return this;
	}

	/// <summary>
	/// 为本次操作附加一条规则（要求公共无参构造）。
	/// </summary>
	/// <typeparam name="TRule">规则类型。</typeparam>
	/// <returns>当前执行器，用于链式调用。</returns>
	public ActuatorBase<TTarget> WithRule<TRule>()
		where TRule : class, IRuleBase, new()
	{
		return WithRule(new TRule());
	}

	/// <summary>
	/// 为本次操作附加一条规则，实例由 <paramref name="provider"/> 解析。
	/// </summary>
	/// <typeparam name="TRule">规则类型。</typeparam>
	/// <param name="provider">用于解析规则实例的服务提供程序。</param>
	/// <returns>当前执行器，用于链式调用。</returns>
	/// <remarks>
	/// 与 <see cref="Rules.AddRule{TRule}(IServiceProvider)"/> 一致：已注册则取注册实例，否则创建新实例。
	/// </remarks>
	public ActuatorBase<TTarget> WithRule<TRule>(IServiceProvider provider)
		where TRule : class, IRuleBase
	{
		Check.EnsureNotNull(provider, nameof(provider));

		return WithRule(ActivatorUtilities.GetServiceOrCreateInstance<TRule>(provider));
	}

	/// <summary>
	/// 为本次操作批量附加规则。
	/// </summary>
	/// <param name="rules">要附加的规则集合；为 <c>null</c> 时不执行任何操作。</param>
	/// <returns>当前执行器，用于链式调用。</returns>
	public ActuatorBase<TTarget> WithRules(IEnumerable<IRuleBase> rules)
	{
		if (rules == null)
		{
			return this;
		}

		foreach (var rule in rules)
		{
			WithRule(rule);
		}

		return this;
	}

	/// <summary>
	/// 在本次操作中绕过指定类型的规则（该规则不参与本次检查）。
	/// </summary>
	/// <typeparam name="TRule">要绕过的规则类型。</typeparam>
	/// <returns>当前执行器，用于链式调用。</returns>
	/// <remarks>
	/// 按<b>精确类型</b>匹配（<c>rule.GetType() == typeof(TRule)</c>），不含派生类型：
	/// 若按可赋值性匹配，<c>BypassRule&lt;RuleBase&gt;()</c> 会连带命中框架自动注入的
	/// <see cref="ScopePolicyRule"/> 等规则，一次笔误就把数据权限信号整体关掉。
	/// 绕过只作用于本对象实例，不影响同类型的其他对象。
	/// </remarks>
	public ActuatorBase<TTarget> BypassRule<TRule>()
		where TRule : IRuleBase
	{
		RuleScope.Exclude(typeof(TRule));
		return this;
	}

	/// <summary>
	/// 在本次操作中按名称绕过规则（该规则不参与本次检查）。
	/// </summary>
	/// <param name="ruleName">规则名称（对应 <see cref="IRuleBase.Name"/>，形如 <c>rule://命名空间.类型名[/属性名]</c>），忽略大小写。</param>
	/// <returns>当前执行器，用于链式调用。</returns>
	public ActuatorBase<TTarget> BypassRule(string ruleName)
	{
		Check.EnsureNotNullOrWhiteSpace(ruleName, nameof(ruleName));

		RuleScope.Exclude(ruleName);
		return this;
	}

	/// <summary>
	/// 让本次操作跳过规则检查。
	/// </summary>
	/// <returns>当前执行器，用于链式调用。</returns>
	/// <remarks>
	/// <para>
	/// <b>这是显式的绕过声明，请只在确有依据时使用</b>（例如数据修复、迁移、管理员强制操作）。
	/// 它只影响本对象本次操作的规则检查，<b>不解除权限</b>：越权仍会由工厂边界抛
	/// <see cref="System.Security.SecurityException"/>。
	/// </para>
	/// <para>
	/// 与 <see cref="BusinessObject.SuspendRuleChecking"/> 分开记录，因此不会覆盖调用方自己挂起的检查状态。
	/// </para>
	/// </remarks>
	public ActuatorBase<TTarget> WithoutRuleChecks()
	{
		RuleScope.SkipAllChecks();
		return this;
	}

	/// <summary>
	/// 让本次删除操作也执行对象级规则。
	/// </summary>
	/// <param name="check">是否检查；默认 <see langword="true"/>。</param>
	/// <returns>当前执行器，用于链式调用。</returns>
	/// <remarks>
	/// <para>
	/// 默认<b>不</b>检查，与 <see cref="ObservableObject{T}.MarkAsDeleted(bool)"/> 的默认值一致：
	/// 越权删除由工厂边界抛 <see cref="System.Security.SecurityException"/>，
	/// 而不是以验证错误的形式出现。开启后删除将改抛
	/// <see cref="Nerosoft.Euonia.Validation.ValidationException"/>——这是有意的行为切换。
	/// </para>
	/// <para>
	/// 需要它的场景：<c>Delete(id).WithRule(...)</c> 附加的规则若要真正执行，必须同时开启本开关，
	/// 否则删除默认不跑规则，附加的规则会静默不执行。
	/// </para>
	/// </remarks>
	public ActuatorBase<TTarget> WithRuleChecksOnDelete(bool check = true)
	{
		CheckObjectRulesOnDelete = check;
		return this;
	}

	/// <summary>
	/// 获取一个值，指示删除操作是否也检查对象级规则。
	/// </summary>
	protected bool CheckObjectRulesOnDelete { get; private set; }

	#endregion

	/// <summary>
	/// 执行完整流程：获取目标对象 → 调用处理程序 → 继续处理 → 终结（保存或执行）。
	/// </summary>
	/// <remarks>
	/// 整个流程通过 <see cref="ActuatorBuilder{TTarget}"/> 的执行管道运行；若构建器启用了工作单元，
	/// 处理逻辑将在事务边界内执行。终结步骤由 <see cref="FinalizeAsync(TTarget, CancellationToken)"/> 决定：
	/// 可编辑对象在状态发生变更（<see cref="ObservableObject{T}.IsChanged"/>）时才保存，命令对象则执行命令体。
	/// 领域事件的自动发布逻辑当前已注释停用。
	/// </remarks>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>处理完成并终结后的目标对象。</returns>
	public async Task<TTarget> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var target = await Factory();

		if (_ruleScope != null)
		{
			// 规则范围在管道之前挂到目标对象上：Handle 块内部若自行触发检查（ValidateAsync、
			// 嵌套 SaveAsync）看到的也是同一套规则；挂载与卸下成对，不残留到调用方后续对该对象的操作。
			target.RuleSet.AttachOperationRules(_ruleScope);
		}

		try
		{
			return await Builder.Pipeline.RunAsync(target, async result =>
			{
				await ContinueHandleAsync(result, cancellationToken);

				// 规则写在终结步骤（保存 / 命令执行）之前，是因为那一步才是规则的权威裁决点：
				// 可编辑对象由 SaveAsync 裁决，命令对象由工厂边界裁决。此处只负责「挂规则」，
				// 让规则进入那一次检查，而不是另跑一遍再把结果并进去——
				// 后者会被 ClearRules 清掉，也会让规则执行两次。
				return await FinalizeAsync(result, cancellationToken);
			});
		}
		finally
		{
			if (_ruleScope != null)
			{
				target.RuleSet.DetachOperationRules();
			}
		}
	}
}