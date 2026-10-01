using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 团队的数据权限模型：<b>成员关系存在子表里</b>（<c>team_member</c>），因此用
/// <see cref="ScopeModelBuilder{T}.MapMany"/> 把「成员」声明成一个集合维度——
/// 「我加入了哪些团队」由数据库实时判定，下推为 <c>EXISTS</c> 相关子查询，
/// 不需要把成员关系反向展开成 id 集合、也不需要把它镜像进授权数据
/// （见 Euonia.Security/README.md §5.9）。
/// <para>
/// 读侧（<c>team:view</c>）用成员维度：成员关系一改，下一次查询即生效。
/// 写侧（编辑 / 删除）只用行内的列（负责人）：单行判定要求对象图完整，
/// 而写路径上的实体通常不带子表——把写侧策略限定在行内列上，就不必为判定去加载成员。
/// </para>
/// </summary>
public sealed class TeamScopeModel : ScopeModel<Team>
{
	public override void Define(ScopeModelBuilder<Team> builder)
	{
		// 行内的列：负责人
		builder.Map(ScopeDimensions.Owner, t => t.LeaderId);

		// 子表（team_member）：成员的用户标识。失效的成员关系不算成员——子表属性写在选择器里，
		// 由数据库实时求值，而不是解析期过滤成的一次性快照。
		builder.MapMany(ScopeDimensions.Member,
		                t => t.Members.Where(m => m.Status == TeamMemberStatus.Active).Select(m => m.UserId));
	}

	/// <summary>默认策略（按码策略之外的兜底）：仅按行内列判定，不要求加载子表。</summary>
	public override ScopePolicy<Team> Policy => ScopePolicy<Team>.Grant(ScopeDimensions.Owner);

	public override void Declare(ScopePolicySet<Team> policies)
	{
		// 读侧：本团队成员或负责人可见。成员维度来自子表 ⇒ 下推为 EXISTS，实时生效。
		policies.ForOperation(BusinessOperation.Read,
			ScopePolicy<Team>.Any(
				ScopePolicy<Team>.Grant(ScopeDimensions.Member),
				ScopePolicy<Team>.Grant(ScopeDimensions.Owner)),
			TeamPermissions.View);

		// 写侧：只用行内的列。单行判定在内存中求值，而写路径上的实体通常不带子表；
		// 把写侧策略限定在行内列上，判定就不依赖对象图是否完整（见 README §5.9 的边界表）。
		policies.ForOperation(BusinessOperation.Update,  ScopePolicy<Team>.Grant(ScopeDimensions.Owner),
			TeamPermissions.Edit);
		policies.ForOperation(BusinessOperation.Delete,  ScopePolicy<Team>.Grant(ScopeDimensions.Owner),
			TeamPermissions.Delete);
	}
}
