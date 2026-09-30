namespace Nerosoft.Euonia.Security;

/// <summary>
/// 操作权限判定：回答「当前用户是否被授予了某个权限码 / 是否属于某个角色」。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类型位于 <c>Euonia.Core</c> 程序集</b>（命名空间沿用 <c>Nerosoft.Euonia.Security</c>，与
/// <see cref="PermissionAttribute"/>、<see cref="IPermissionCodeSource"/> 同类）：
/// 「用户有没有这个权限」是权限的<b>基础问题</b>，宿主框架（工厂边界、业务方法内的分支）与鉴权实现
/// 都只回答它，彼此不必认识。
/// </para>
/// <para>
/// <b>实现者只需实现两个成员</b>（<see cref="IsGranted"/>、<see cref="IsInRole"/>）：
/// 组合语义（任一 / 要求整体）与异步入口都有默认实现，且默认实现是按「未认证 ⇒ 未授予」的保守口径写的。
/// 需要按请求缓存授权数据、或角色不来自声明时，覆写对应成员即可。
/// </para>
/// <para>
/// <b>实现必须 fail-closed</b>：拿不到用户、拿不到授权数据时返回 <see langword="false"/>；
/// 空权限码 / 空角色表示「该项不作要求」，返回 <see langword="true"/>。
/// </para>
/// </remarks>
public interface IPermissionChecker
{
	/// <summary>
	/// 当前用户是否持有指定权限码；空权限码视为不作要求。
	/// </summary>
	/// <param name="permission">权限码；支持以 <c>*</c> 结尾的前缀通配（由实现决定口径）。</param>
	/// <returns>持有则返回 <see langword="true"/>。</returns>
	bool IsGranted(string permission);

	/// <summary>
	/// 当前用户是否属于指定角色；空角色视为不作要求。
	/// </summary>
	/// <param name="role">角色名。</param>
	/// <returns>属于则返回 <see langword="true"/>。</returns>
	bool IsInRole(string role);

	/// <summary>
	/// 当前用户是否持有给定权限码中的任意一个；<paramref name="permissions"/> 为空时返回 <see langword="false"/>。
	/// </summary>
	/// <param name="permissions">权限码数组。</param>
	/// <returns>任意一个通过则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 仓库与示例中没有任何调用点（判定入口 <see cref="IsRequirementSatisfied"/> 只消费
	/// <see cref="IsGranted"/> 与 <see cref="IsInAnyRole"/>）。保留默认实现以兼容已发布的公开表面，
	/// 但标记 <see cref="ObsoleteAttribute"/> 提示新代码改用 <c>permissions.Any(IsGranted)</c>——
	/// 与 <see cref="IsInAnyRole"/> 不同，它没有被任何组合语义依赖。
	/// </remarks>
	[Obsolete("仓库内无调用点；如需「任一通过」语义请直接 permissions.Any(IsGranted)。")]
	bool IsGrantedAny(params string[] permissions)
	{
		return permissions?.Any(IsGranted) == true;
	}

	/// <summary>
	/// 当前用户是否属于给定角色中的任意一个；<paramref name="roles"/> 为空时返回 <see langword="false"/>。
	/// </summary>
	/// <param name="roles">角色数组。</param>
	/// <returns>任意一个通过则返回 <see langword="true"/>。</returns>
	bool IsInAnyRole(params string[] roles)
	{
		return roles?.Any(IsInRole) == true;
	}

	/// <summary>
	/// 当前用户是否同时满足给定的权限码与角色要求；两部分各自为空表示该项不作要求，角色之间是「或」。
	/// </summary>
	/// <param name="permission">权限码；为空表示不要求权限。</param>
	/// <param name="roles">允许的角色；为空表示不限制角色。</param>
	/// <returns>全部有效要求都满足则返回 <see langword="true"/>。</returns>
	bool IsRequirementSatisfied(string permission, string[] roles)
	{
		var rolesSatisfied = roles is not { Length: > 0 } || IsInAnyRole(roles);
		var permissionSatisfied = string.IsNullOrEmpty(permission) || IsGranted(permission);

		return rolesSatisfied && permissionSatisfied;
	}

	/// <summary>
	/// 当前用户是否持有指定权限码（异步）。
	/// </summary>
	/// <param name="permission">权限码。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>持有则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 默认实现直接调用 <see cref="IsGranted"/>。授权数据需要异步解析（例如查库）的实现应当覆写它，
	/// 以免调用方在同步路径上阻塞线程。
	/// </remarks>
	ValueTask<bool> IsGrantedAsync(string permission, CancellationToken cancellationToken = default)
	{
		return new ValueTask<bool>(IsGranted(permission));
	}

	/// <summary>
	/// 确保判定所需的授权数据已解析（异步；幂等，已解析时立即返回）。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <remarks>
	/// <para>
	/// 本方法<b>只做预热，不做判定</b>：供宿主框架的<b>异步</b>授权路径（<c>BusinessObjectFactory</c> 的
	/// <c>*Async</c> 入口）在调用同步判定（<see cref="IsGranted"/>）之前把授权数据解析出来——
	/// 否则首次判定会退化成 sync-over-async，在负载下表现为线程池饥饿。
	/// </para>
	/// <para>
	/// 默认实现是<b>空操作</b>：授权数据本就同步可用、或不需要预热的实现无需改动即可继续编译。
	/// 授权数据由 <c>IScopeGuard</c> 异步解析的实现应当覆写，且<b>必须容忍解析器缺席</b>
	/// （抛 <see cref="InvalidOperationException"/> 而不是吞掉会让预热阶段把「拒绝」变成 500；
	/// 接线错误由首次解析 <c>IScopeGuard</c> 时的启动校验负责暴露）。
	/// </para>
	/// </remarks>
	ValueTask EnsureResolvedAsync(CancellationToken cancellationToken = default)
	{
		// netstandard2.1 没有 ValueTask.CompletedTask（.NET 5 才引入）
		return default;
	}
}
