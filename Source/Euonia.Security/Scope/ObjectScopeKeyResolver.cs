namespace Nerosoft.Euonia.Security;

/// <summary>
/// 由对象状态推断策略键，实现 <see cref="IScopeKeyResolver"/>。
/// </summary>
/// <remarks>
/// <para>
/// 「资源当前代表哪个操作」是<b>宿主框架</b>的知识，因此这里不猜：向容器中的
/// <see cref="IObjectOperationResolver"/> 要答案（宿主框架注册的那一个），
/// 再按操作解析策略键。契约缺席时（例如宿主框架没注册映射器）回落到
/// <see cref="ScopeKeys.Default"/>——「对象没有待执行操作」与「没人回答这个问题」都不该抛异常，
/// 真正的拦截发生在工厂边界。
/// </para>
/// <para>
/// 本类与宿主框架之间没有任何程序集依赖：它只认识 Core 的契约，宿主框架只认识它自己的对象模型。
/// </para>
/// </remarks>
internal sealed class ObjectScopeKeyResolver(ScopeModelRegistry registry, IPermissionCodeSource codeSource, IObjectOperationResolver operationResolver) : IScopeKeyResolver
{
	/// <inheritdoc />
	public string Resolve(object resource, string explicitKey)
	{
		if (!string.IsNullOrWhiteSpace(explicitKey))
		{
			return explicitKey;
		}

		if (resource == null
		    || operationResolver == null
		    || !operationResolver.TryResolve(resource, out var operation))
		{
			return ScopeKeys.Default;
		}

		return registry.TryGetInherited(resource.GetType(), out var registration)
			? ScopeKeyResolver.Resolve(registration, registration.Descriptor.ResourceType, operation, codeSource)
			: ScopeKeys.For(operation);
	}
}
