using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 团队的数据权限模型：默认策略是「本团队成员或本团队负责人可见」，
/// 编辑、删除按码收敛为「仅负责人可操作」——负责人维度由 <see cref="ScopeSubjectSetBuilder.AddSelf"/>
/// 提供（owner 维度），因此与组件的外部队长（如 T-2 的 u-0）无关。
/// 行级判定在工厂边界自动执行，读侧列表由 <see cref="IScopeGuard.Apply{T}"/> 下推。
/// </summary>
public sealed class TeamScopeModel : ScopeModel<Team>
{
	public override void Define(ScopeModelBuilder<Team> builder)
	{
		builder.Map(ScopeDimensions.Team, t => t.Id);
		builder.Map(ScopeDimensions.Owner, t => t.LeaderId);
	}

	public override ScopePolicy<Team> Policy
		=> ScopePolicy<Team>.Any(
			ScopePolicy<Team>.Grant(ScopeDimensions.Team),
			ScopePolicy<Team>.Grant(ScopeDimensions.Owner));

	public override void Declare(ScopePolicySet<Team> policies)
	{
		policies.For(TeamPermissions.Edit, ScopePolicy<Team>.Grant(ScopeDimensions.Owner));
		policies.For(TeamPermissions.Delete, ScopePolicy<Team>.Grant(ScopeDimensions.Owner));
	}
}