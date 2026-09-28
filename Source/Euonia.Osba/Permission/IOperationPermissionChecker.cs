namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 操作权限判定：回答「当前用户是否满足给定的权限要求」。
/// </summary>
/// <remarks>
/// <para>
/// <b>由宿主的权限实现提供</b>：想接策略引擎时用 <c>Euonia.Osba.Security</c> 的 <c>AddObjectPermission</c>，
/// 也可以注册自己的实现——读配置、查权限表、或接上宿主已有的鉴权框架。
/// 工厂边界与业务对象只认本接口，不认识任何具体的鉴权实现。
/// </para>
/// <para>
/// <b>实现必须 fail-closed</b>：拿不到用户、拿不到授权数据时一律返回 <see langword="false"/>。
/// 本库不接受「判定不了就放行」——那是静默放行，比拒绝危险得多。
/// </para>
/// <para>
/// <b>判定口径</b>（决定宽严，自定义实现必须照做；括号内是引擎默认实现的对应语义）：
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IsGranted"/>：是否持有权限码。权限码<b>大小写不敏感</b>、
/// 支持末尾 <c>*</c> 前缀通配（持 <c>repo:*</c> 可过 <c>repo:push</c>）；权限码为空视为不作要求，返回
/// <see langword="true"/>；用户未认证时返回 <see langword="false"/>。</description></item>
/// <item><description><see cref="IsInRole"/>：角色来自主体声明（<c>ClaimsPrincipal.IsInRole</c>），
/// 命中任一即真；角色名为空时返回 <see langword="true"/>。</description></item>
/// <item><description><see cref="IsRequirementSatisfied"/>：权限码与角色<b>同时</b>满足才为真；
/// 两者分别为空表示该项不作要求（多角色之间是「或」）。工厂边界会把一个操作上的<b>全部</b>要求逐个交给
/// 本方法，全部为真才放行。</description></item>
/// <item><description><see cref="IsGrantedAsync"/>：与 <see cref="IsGranted"/> 同义，供实现异步解析授权数据
/// （引擎路径的解析结果按请求缓存，因此重复调用不会重复查库）。</description></item>
/// </list>
/// </remarks>
public interface IOperationPermissionChecker
{
	/// <summary>
	/// 当前用户是否持有指定权限码。
	/// </summary>
	/// <param name="permission">权限码；为空时返回 <see langword="true"/>。</param>
	/// <returns>持有则返回 <see langword="true"/>。</returns>
	bool IsGranted(string permission);

	/// <summary>
	/// 当前用户是否属于指定角色。
	/// </summary>
	/// <param name="role">角色名；为空时返回 <see langword="true"/>。</param>
	/// <returns>属于则返回 <see langword="true"/>。</returns>
	bool IsInRole(string role);

	/// <summary>
	/// 当前用户是否同时满足给定的权限码与角色要求。
	/// </summary>
	/// <param name="permission">权限码；为空表示不要求权限。</param>
	/// <param name="roles">允许的角色；为空表示不限制角色，多角色之间为「或」。</param>
	/// <returns>满足则返回 <see langword="true"/>。</returns>
	bool IsRequirementSatisfied(string permission, IReadOnlyList<string> roles);

	/// <summary>
	/// 当前用户是否持有指定权限码（异步，允许实现按需解析授权数据）。
	/// </summary>
	/// <param name="permission">权限码。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>持有则返回 <see langword="true"/>。</returns>
	ValueTask<bool> IsGrantedAsync(string permission, CancellationToken cancellationToken = default);
}
