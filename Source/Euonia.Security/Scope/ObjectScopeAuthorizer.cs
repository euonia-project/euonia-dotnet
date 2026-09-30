using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 引擎实现的行级数据权限：判定交给 <see cref="IScopeGuard"/>，策略键按行级模型的注册项解析。
/// </summary>
/// <remarks>
/// <para>
/// 注册为<b>单例</b>：<see cref="IsConstrained"/> 可能在对象未接入作用域时被调用
/// （宿主框架据此决定「报错」还是「放行」），因此它只依赖单例服务（模型注册表、权限码来源）。
/// </para>
/// <para>
/// 判定从调用方传入的 <c>scope</c>（对象自己的服务提供程序）解析 <see cref="IScopeGuard"/>，
/// 保证用的是本请求的用户；<b>不</b>依赖环境上下文——那可能取到另一个请求的用户。
/// </para>
/// <para>
/// 本类与宿主框架之间没有任何程序集依赖：契约（<see cref="IObjectScopeAuthorizer"/>）在
/// <c>Euonia.Core</c>，由引擎实现自己懂的那一半，宿主框架实现「对象状态 → 操作」那一半。
/// </para>
/// </remarks>
internal sealed class ObjectScopeAuthorizer(ScopeModelRegistry registry, IPermissionCodeSource codeSource) : IObjectScopeAuthorizer
{
	/// <inheritdoc />
	public bool IsConstrained(Type resourceType)
	{
		// 「声明了模型」才受约束：没有任何模型的应用（或未声明模型的类型）不受数据权限限制
		return registry.HasDeclarations && registry.IsDeclared(resourceType);
	}

	/// <inheritdoc />
	public bool Allows(object target, string operation, IServiceProvider scope)
	{
		return Guard(scope).AllowsObject(target, KeyFor(target, operation));
	}

	/// <inheritdoc />
	public string Explain(object target, string operation, IServiceProvider scope)
	{
		return Guard(scope).ExplainObject(target, KeyFor(target, operation));
	}

	/// <inheritdoc />
	public bool AllowsRow(object target, string scopeKey, IServiceProvider scope)
	{
		// 传入的是权限码：为空时由 IScopeKeyResolver 按对象状态推断
		return Guard(scope).AllowsObject(target, scopeKey);
	}

	/// <inheritdoc />
	public string ExplainRow(object target, string scopeKey, IServiceProvider scope)
	{
		return Guard(scope).ExplainObject(target, scopeKey);
	}

	/// <inheritdoc />
	/// <remarks>
	/// 覆写默认（空操作）实现：判定要读授权数据（<see cref="AllowsObject"/> → <c>GetSubjects</c>），
	/// 异步授权路径先预热，避免冷缓存时阻塞调用线程。作用域取调用方传入的那一个，与 <see cref="Allows"/> 同口径。
	/// </remarks>
	public async ValueTask EnsureResolvedAsync(IServiceProvider scope, CancellationToken cancellationToken = default)
	{
		var guard = scope?.GetService<IScopeGuard>();

		if (guard == null)
		{
			return;
		}

		try
		{
			await guard.EnsureResolvedAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException)
		{
			// 解析器缺席：交回同步判定 fail-closed（<see cref="Guard"/> 仍会报配置错误），预热不改写行为
		}
	}

	private IScopeGuard Guard(IServiceProvider scope)
	{
		var guard = scope?.GetService<IScopeGuard>();

		Check.Ensure(
			guard != null,
			Resources.IDS_SCOPE_GUARD_NOT_RESOLVED,
			nameof(IScopeGuard));

		return guard;
	}

	/// <summary>
	/// 按<b>操作</b>解析策略键：操作由调用方（工厂边界）给出，不从对象状态推断
	/// （判定可能发生在业务方法返回之后，此时对象状态已被清干净）。
	/// </summary>
	private string KeyFor(object target, string operation)
	{
		return ScopeKeyResolver.Resolve(registry, target.GetType(), operation, codeSource);
	}
}
