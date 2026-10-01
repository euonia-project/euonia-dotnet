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
	/// 授权数据本就同步可用的实现，直接返回已完成的 <see cref="ValueTask"/> 即可。实现<b>必须容忍解析器缺席</b>
	/// （抛 <see cref="InvalidOperationException"/> 而不是吞掉，会让预热阶段把「拒绝」变成 500；
	/// 接线错误由首次解析 <c>IScopeGuard</c> 时的启动校验负责暴露）。
	/// </para>
	/// </remarks>
	ValueTask EnsureResolvedAsync(CancellationToken cancellationToken = default);
}
