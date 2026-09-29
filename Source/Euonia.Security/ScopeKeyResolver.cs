namespace Nerosoft.Euonia.Security;

/// <summary>
/// 策略键解析的<b>唯一出口</b>：由业务操作与声明的权限码解析出行级策略要用的键。
/// </summary>
/// <remarks>
/// <para>
/// 键<b>只由操作决定</b>：资源状态的改变只是改变了实际执行的操作，而每个操作各自应用自己的策略。
/// </para>
/// <para>
/// 解析优先级：<b>声明了权限码且模型为该码声明了策略 → 用该码</b>；否则用该操作的默认键
/// （见 <see cref="ScopeKeys.For"/>）。完整判定树与歧义规则见 README §5.8。
/// </para>
/// </remarks>
public static class ScopeKeyResolver
{
	/// <summary>
	/// 解析指定资源实例类型在指定操作上的策略键：沿基类链找到注册项后交给 <see cref="Resolve(ScopeModelRegistration, Type, string, IPermissionCodeSource)"/>。
	/// </summary>
	/// <param name="registry">模型注册表。</param>
	/// <param name="resourceType">资源实例类型（实体框架的代理类型是派生类，会沿基类链查找）。</param>
	/// <param name="operation">业务操作名。</param>
	/// <param name="codeSource">权限码来源，由使用方提供，见 <see cref="IPermissionCodeSource"/>。</param>
	/// <returns>策略键；类型未声明模型时返回该操作的默认键。</returns>
	/// <exception cref="InvalidOperationException">同一操作解析出多个有策略的权限码时抛出。</exception>
	/// <remarks>
	/// 「先沿基类链找注册项，找不到就回落到 <see cref="ScopeKeys.For"/>」这个回落组合此前在
	/// <c>ObjectScopeAuthorizer</c> 与 <c>ObjectScopeKeyResolver</c> 各写了一遍，
	/// 收敛到这里后，回落口径只需要维护一处。
	/// </remarks>
	public static string Resolve(ScopeModelRegistry registry, Type resourceType, string operation, IPermissionCodeSource codeSource)
	{
		registry.TryGetInherited(resourceType, out var registration);

		return Resolve(registration, registration?.Descriptor.ResourceType, operation, codeSource);
	}

	/// <summary>
	/// 解析指定类型在指定操作上的策略键。
	/// </summary>
	/// <param name="registration">资源类型的注册项；为 <see langword="null"/> 时返回该操作的默认键。</param>
	/// <param name="resourceType">资源类型。</param>
	/// <param name="operation">业务操作名。</param>
	/// <param name="codeSource">权限码来源，由使用方提供，见 <see cref="IPermissionCodeSource"/>。</param>
	/// <returns>策略键。</returns>
	/// <exception cref="InvalidOperationException">同一操作解析出多个有策略的权限码时抛出。</exception>
	public static string Resolve(ScopeModelRegistration registration, Type resourceType, string operation, IPermissionCodeSource codeSource)
	{
		if (registration == null)
		{
			return ScopeKeys.For(operation);
		}

		var matched = codeSource.CodesFor(resourceType, operation)
		                        .Where(code => registration.PolicyFor(code) != null)
		                        .ToArray();

		Check.Ensure(matched.Length <= 1,
			"资源类型 '{0}' 的操作 {1} 解析出多个声明了行级策略的权限码（{2}）。请确保同一操作最多只有一个权限码声明了策略。",
			resourceType.Name,
			operation,
			string.Join(", ", matched));

		// 命中权限码的策略胜出；否则用该操作的默认键（模型未为该键声明时，调用方回落到默认策略）
		return matched.Length == 1 ? matched[0] : ScopeKeys.For(operation);
	}
}