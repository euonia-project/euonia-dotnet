namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 策略键解析的<b>唯一出口</b>：由业务操作与声明的权限码解析出行级策略要用的键。
/// </summary>
/// <remarks>
/// <para>
/// 键<b>只由操作决定</b>，操作只由 <see cref="ScopeOperationMap"/> 这一条映射决定。
/// 键不受调用方状态直接影响——<c>MarkAsNew</c>/<c>MarkAsChanged</c>/<c>MarkAsDeleted</c>
/// 改变的是实际执行的操作，而每个操作各自应用自己的策略。
/// </para>
/// <para>
/// 解析优先级：<b>声明了权限码且模型为该码声明了策略 → 用该码</b>；
/// 否则用该操作的默认键（<see cref="ScopeKeys.For"/>）。
/// 若同一操作解析出<b>多个</b>都有策略的码，属配置歧义，直接抛错——
/// 不允许「实际生效的是哪一个」靠猜。
/// </para>
/// </remarks>
internal static class ScopeKeyResolver
{
	/// <summary>
	/// 解析指定类型在指定操作上的策略键。
	/// </summary>
	/// <param name="registration">资源类型的注册项；为 <see langword="null"/> 时返回该操作的默认键。</param>
	/// <param name="resourceType">资源类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>策略键。</returns>
	/// <exception cref="InvalidOperationException">同一操作解析出多个有策略的权限码时抛出。</exception>
	internal static string Resolve(ScopeModelRegistration registration, Type resourceType, BusinessOperation operation)
	{
		if (registration == null)
		{
			return ScopeKeys.For(operation);
		}

		var matched = PermissionRequirements.CodesFor(resourceType, operation)
		                                      .Where(code => registration.PolicyFor(code) != null)
		                                      .ToArray();

		Check.Ensure(
			matched.Length <= 1,
			"资源类型 '{0}' 的操作 {1} 解析出多个声明了行级策略的权限码（{2}）。请确保同一操作最多只有一个权限码声明了策略。",
			resourceType.Name,
			operation,
			string.Join(", ", matched));

		// 命中权限码的策略胜出；否则用该操作的默认键（模型未为该键声明时，调用方回落到默认策略）
		return matched.Length == 1 ? matched[0] : ScopeKeys.For(operation);
	}
}
