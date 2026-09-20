namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 断言当前对象<b>这一行</b>是否在当前用户的数据范围内（按当前操作解析策略键）的对象级规则。
/// </summary>
/// <remarks>
/// <para>
/// 本规则由框架在 <c>BusinessObject.InitializeRules</c> 中对已声明权限模型的类型<b>自动注入</b>，
/// 使越权保存以验证错误的形式在保存前暴露；也可以手工注册。
/// </para>
/// <para>
/// <b>本规则不是强制点</b>：规则可被 <see cref="Rules.SuppressRuleChecking"/> 跳过，
/// 且对象级规则在 <c>IsDeleted</c> 时默认不执行（越权<b>删除</b>仍由工厂边界抛
/// <see cref="System.Security.SecurityException"/>）。工厂边界始终是权威强制点。
/// </para>
/// <para>
/// 规则实例是<b>按类型共享的进程级单例</b>，因此本类<b>不持有任何状态</b>——
/// 注册表、授权数据、策略键、操作全部在执行时从 <see cref="IRuleContext.Target"/> 实时解析。
/// 这一点是必须的：注册表是按容器的，而规则集合是进程级的。
/// </para>
/// </remarks>
public sealed class ScopePolicyRule : RuleBase
{
	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		// 任何一环缺失都静默放行：规则是补充信号，不是强制点，绝不能在这里抛异常
		// （Rules.RunAsync 会把异常转成错误，从而让「未接入权限体系」被误报成越权）。
		if (context.Target is not IBusinessObject businessObject)
		{
			return Task.CompletedTask;
		}

		var businessContext = businessObject.BusinessContext;
		if (businessContext == null)
		{
			return Task.CompletedTask;
		}

		var registry = businessContext.GetService<ScopeModelRegistry>();
		if (registry == null || !registry.IsDeclared(context.Target.GetType()))
		{
			return Task.CompletedTask;
		}

		var guard = businessContext.GetService<IScopeGuard>();
		if (guard == null)
		{
			return Task.CompletedTask;
		}

		// 传入 null 让 guard 按目标对象当前的状态→操作解析策略键，
		// 与工厂边界共用 ScopeKeyResolver，避免两处漂移
		if (!guard.AllowsObject(context.Target))
		{
			context.AddErrorResult($"当前用户无权执行该操作（数据范围之外）。{guard.ExplainObject(context.Target)}");
		}

		return Task.CompletedTask;
	}
}
