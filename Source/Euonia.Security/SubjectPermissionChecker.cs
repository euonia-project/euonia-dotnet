namespace Nerosoft.Euonia.Security;

/// <summary>
/// 默认的操作权限检查器：权限码<b>从授权数据实时解析</b>，角色仍来自声明。
/// </summary>
/// <remarks>
/// <para>
/// 权限码<b>不放在令牌里</b>（理由见 DESIGN §1.2）：它由 <see cref="IScopeSubjectResolver"/> 实时解析、随 <see cref="IScopeGuard"/> 按请求缓存。
/// </para>
/// <para>
/// 因此<b>撤销的生效时机是「下一次解析」</b>（通常是下一个请求）；同一作用域内需要立即生效时显式调用
/// <see cref="IScopeGuard.Refresh"/>，全程不需要重新签发令牌（见 README §5.5）。
/// </para>
/// <para>
/// 角色仍走 <see cref="UserPrincipal.IsInRole"/>（来自声明）：角色数量少而稳定，不构成令牌膨胀问题；<b>细粒度授权请一律使用权限码</b>。
/// </para>
/// </remarks>
public class SubjectPermissionChecker : IPermissionChecker
{
	private readonly UserPrincipal _user;
	private readonly IScopeGuard _guard;

	/// <summary>
	/// 初始化 <see cref="SubjectPermissionChecker"/> 的新实例。
	/// </summary>
	/// <param name="user">当前登录用户；为 <see langword="null"/> 时所有权限码判定为未授权。</param>
	/// <param name="guard">授权数据入口，提供当前用户持有的权限码。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="guard"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <remarks><paramref name="user"/> 允许为 <see langword="null"/>（匿名视为全部未授予）；<paramref name="guard"/> 不允许——它缺席只能是接线错误。</remarks>
	public SubjectPermissionChecker(UserPrincipal user, IScopeGuard guard)
	{
		Check.EnsureNotNull(guard, nameof(guard));

		_user = user;
		_guard = guard;
	}

	/// <inheritdoc />
	public bool IsGranted(string permission)
	{
		if (string.IsNullOrEmpty(permission))
		{
			return true;
		}

		var user = _user;
		if (user == null || !user.IsAuthenticated)
		{
			return false;
		}

		return Holds(permission);
	}

	/// <inheritdoc />
	public bool IsInRole(string role)
	{
		return _user is { IsAuthenticated: true } && _user.IsInRole(role);
	}

	/// <inheritdoc />
	/// <remarks>
	/// 覆写默认实现：授权数据按请求解析是异步的，走这里可以避免同步路径上的 sync-over-async
	/// （默认实现会直接调用 <see cref="IsGranted"/>，那会在首次判定时阻塞线程）。
	/// </remarks>
	public async ValueTask<bool> IsGrantedAsync(string permission, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(permission))
		{
			return true;
		}

		if (_user is not { IsAuthenticated: true })
		{
			return false;
		}

		try
		{
			await _guard.EnsureResolvedAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		return Holds(permission);
	}

	/// <summary>
	/// 取当前用户持有的权限码并判定；授权数据取不到时返回 <see langword="false"/>。
	/// </summary>
	/// <remarks>
	/// <see cref="IPermissionChecker"/> 明确要求「拿不到用户、拿不到授权数据时返回 <see langword="false"/>」，
	/// 所以这里吞掉 <see cref="ScopeGuard"/> 在解析器缺席时抛的 <see cref="InvalidOperationException"/>——
	/// 把接线错误抛进调用方的判定分支，得到的是 500 而不是「拒绝」，既不比拒绝更安全，也不符合契约。
	/// 这不等于把接线错误悄悄藏起来：<c>provider.ValidatePermissionSetup()</c> 在启动期就会把它抛出来，
	/// 而行级数据权限那条路径（直接用 <see cref="IScopeGuard"/>）仍照旧抛出、不会静默放行。
	/// </remarks>
	private bool Holds(string permission)
	{
		try
		{
			// 支持以 * 结尾的前缀通配（例如持有 repo:* 可通过 repo:push 的类型级闸门）
			return _guard.GetSubjects().HoldsPermission(permission);
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}
}
