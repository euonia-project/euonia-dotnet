using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 解析当前用户被授予的数据范围值（数据权限的值来源）。
/// </summary>
/// <remarks>
/// <para>
/// 数据权限的基本原则：值（用户属于哪些团队、能访问哪些资源等）是运行期数据，
/// 必须在每次判定时从应用数据实时解析，绝不能固化成声明/Token/代码字面量——
/// 团队成员关系和资源授权随时可能调整，团队、资源本身也会有新建删除，都要立即生效。
/// </para>
/// <para>
/// 本接口由应用实现，通常通过查询授权数据（例如成员关系表、团队-资源关联表、
/// 资源授权表等）返回用户当前被授予的范围标签；框架不预设任何维度与值格式，
/// 也不为值提供默认来源。
/// </para>
/// </remarks>
public interface IUserScopeProvider
{
	/// <summary>
	/// 解析指定用户当前被授予的全部范围标签。
	/// </summary>
	/// <param name="user">当前用户。</param>
	/// <returns>用户当前被授予的范围标签序列（各维度混合，由判定引擎按维度分组使用）；无任何范围时返回空序列。</returns>
	IReadOnlyList<ScopeTag> ResolveScopes(UserPrincipal user);
}