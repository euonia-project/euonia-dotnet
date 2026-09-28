using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Security;

/// <summary>
/// 引擎实现的操作权限判定：权限码来自授权数据（<see cref="IPermissionChecker"/>），
/// 角色来自主体声明，异步入口按请求缓存解析结果。
/// </summary>
internal sealed class EngineOperationPermissionChecker(IPermissionChecker checker, IScopeGuard guard) : IOperationPermissionChecker
{
	/// <inheritdoc />
	public bool IsGranted(string permission)
	{
		return checker.IsGranted(permission);
	}

	/// <inheritdoc />
	public bool IsInRole(string role)
	{
		return checker.IsInRole(role);
	}

	/// <inheritdoc />
	public bool IsRequirementSatisfied(string permission, IReadOnlyList<string> roles)
	{
		return checker.IsRequirementSatisfied(permission, roles is { Count: > 0 } ? [.. roles] : []);
	}

	/// <inheritdoc />
	/// <remarks>
	/// 先确保授权数据已解析（按请求只解析一次），再按与同步入口相同的口径判定：
	/// 未认证用户的授权数据在解析期即为空集合，因此两条路径都 fail-closed。
	/// </remarks>
	public async ValueTask<bool> IsGrantedAsync(string permission, CancellationToken cancellationToken = default)
	{
		await guard.EnsureResolvedAsync(cancellationToken).ConfigureAwait(false);

		return guard.GetSubjects().HoldsPermission(permission);
	}
}
