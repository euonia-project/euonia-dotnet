using System.Security.Claims;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 解析当前用户被授予的数据权限主体（数据权限的「值来源」），由应用实现。
/// </summary>
/// <remarks>
/// 数据权限的基本原则：<b>值（用户属于哪些部门、能访问哪些资源）是运行期数据</b>，必须在每次判定时从应用数据解析，绝不能固化成声明、令牌或代码字面量（见 README §4.2）。
/// <b>层级要在解析期展开</b>：例如「本部门及所有下级」，应把整棵子树展开为扁平集合再交给 <see cref="ScopeSubjectSetBuilder.AddRange"/>，框架不需要理解层级（见 DESIGN §1.5）。
/// 若需要「本人可访问」，必须调用 <see cref="ScopeSubjectSetBuilder.AddSelf"/>——<see cref="ScopePolicy{T}.Self"/> 并不特殊，它等价于 <c>Grant(owner)</c>。
/// 解析结果按请求缓存（见 <see cref="IScopeGuard"/>），因此本方法每个请求只会被调用一次；不要把长生命周期缓存写在这里（见 README §5.5）。
/// </remarks>
public interface IScopeSubjectResolver
{
	/// <summary>
	/// 解析指定用户当前被授予的全部主体。
	/// </summary>
	/// <param name="user">当前用户，可能为 <see langword="null"/>（未接入用户上下文）。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>用户被授予的主体集合；无任何授予时返回空集合。</returns>
	ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
