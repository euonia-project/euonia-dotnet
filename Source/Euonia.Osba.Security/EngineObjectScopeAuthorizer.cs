using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Security;

/// <summary>
/// 引擎实现的行级数据权限：把判定交给 <see cref="IScopeGuard"/>，策略键按行级模型的注册项解析。
/// </summary>
/// <remarks>
/// <para>
/// 注册为<b>单例</b>：<see cref="IsConstrained"/> 可能在对象未接线、容器里没有请求作用域时被调用
/// （工厂边界据此决定「报错」还是「放行」），因此它只依赖单例服务（注册表、权限码来源）。
/// </para>
/// <para>
/// 判定方法从传入的 <see cref="BusinessContext"/> 解析 <see cref="IScopeGuard"/>
/// （作用域内的那一个，保证用的是本请求的用户），而<b>不</b>依赖环境上下文——
/// 那可能取到另一个请求的用户。
/// </para>
/// </remarks>
internal sealed class EngineObjectScopeAuthorizer(ScopeModelRegistry registry, IPermissionCodeSource codeSource) : IObjectScopeAuthorizer
{
	/// <inheritdoc />
	public bool IsConstrained(Type resourceType)
	{
		// 「声明了模型」才受约束：没有任何模型的应用（或未声明模型的类型）不受数据权限限制
		return registry.HasDeclarations && registry.IsDeclared(resourceType);
	}

	/// <inheritdoc />
	public bool AllowsOperation(BusinessContext context, object target, string operation)
	{
		return Guard(context).AllowsObject(target, ResolveKey(target, operation));
	}

	/// <inheritdoc />
	public string ExplainOperation(BusinessContext context, object target, string operation)
	{
		return Guard(context).ExplainObject(target, ResolveKey(target, operation));
	}

	/// <inheritdoc />
	public bool AllowsRow(BusinessContext context, object target, string scopeKey = null)
	{
		// 传入的是权限码：为空时由 IScopeKeyResolver 按对象状态推断（见 ObjectScopeKeyResolver）
		return Guard(context).AllowsObject(target, scopeKey);
	}

	/// <inheritdoc />
	public string ExplainRow(BusinessContext context, object target, string scopeKey = null)
	{
		return Guard(context).ExplainObject(target, scopeKey);
	}

	private IScopeGuard Guard(BusinessContext context)
	{
		var guard = context?.GetService<IScopeGuard>();

		Check.Ensure(
			guard != null,
			"资源类型受数据权限约束，但无法解析 {0}。请确认已调用 AddObjectPermission（或 AddPermission）。",
			nameof(IScopeGuard));

		return guard;
	}

	/// <summary>
	/// 按<b>操作</b>解析策略键：操作由工厂边界给出，不从对象状态推断
	/// （判定可能发生在业务方法返回之后，此时对象状态已被清干净）。
	/// </summary>
	private string ResolveKey(object target, string operation)
	{
		return registry.TryGetInherited(target.GetType(), out var registration)
			? ScopeKeyResolver.Resolve(registration, registration.Descriptor.ResourceType, operation, codeSource)
			: ScopeKeys.For(operation);
	}
}
