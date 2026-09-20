namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 断言当前用户持有指定权限码的对象级规则。
/// </summary>
/// <remarks>
/// <para>
/// 用于让权限失败以<b>验证错误</b>的形式回到调用方（表单可展示字段级错误），
/// 而不是抛出 <see cref="System.Security.SecurityException"/>。
/// </para>
/// <para>
/// 本规则<b>不是强制点</b>：<see cref="Rules.SuppressRuleChecking"/> 与
/// <c>BusinessObject.BypassRuleChecks</c> 都能跳过规则，且对象级规则只在保存时执行。
/// 真正的强制点在 <c>BusinessObjectFactory</c> 的调用边界。
/// </para>
/// <para>
/// 规则实例是<b>按类型共享的进程级单例</b>，因此本类不持有任何状态。
/// </para>
/// </remarks>
public sealed class PermissionRule : RuleBase
{
	private readonly string _permission;

	/// <summary>
	/// 初始化 <see cref="PermissionRule"/> 的新实例。
	/// </summary>
	/// <param name="permission">要断言的权限码。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="permission"/> 为空白时抛出。</exception>
	public PermissionRule(string permission)
	{
		Check.EnsureNotNullOrWhiteSpace(permission, nameof(permission));

		_permission = permission;
	}

	/// <summary>
	/// 获取本规则断言的权限码。
	/// </summary>
	public string Permission => _permission;

	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		if (context.Target is not IBusinessObject businessObject)
		{
			return Task.CompletedTask;
		}

		// 权限检查器按作用域从当前业务上下文解析；未注册时不判定（与 CanXObject 的约定一致），
		// 「声明了权限却没接解析器」由启动期校验负责暴露。
		var checker = businessObject.BusinessContext?.GetService<IPermissionChecker>();

		if (checker != null && !checker.IsGranted(_permission))
		{
			context.AddErrorResult($"缺少所需权限：{_permission}。");
		}

		return Task.CompletedTask;
	}
}
