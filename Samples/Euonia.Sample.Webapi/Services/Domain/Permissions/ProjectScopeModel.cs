using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 项目的数据权限模型：默认策略是「本人或本部成员可见」，
/// 并按 <see cref="ProjectPermissions.Edit"/> 收窄为「仅所有者可编辑」。
/// 行级判定在工厂边界自动执行（fetch/update 越权时抛出
/// <see cref="System.Security.SecurityException"/>），读侧列表由
/// <see cref="IScopeGuard.Apply{T}"/> 下推。
/// </summary>
public sealed class ProjectScopeModel : ScopeModel<Project>
{
	public override void Define(ScopeModelBuilder<Project> builder)
	{
		builder.Map(ScopeDimensions.Owner, p => p.OwnerId);
		builder.Map(ScopeDimensions.Dept, p => p.DeptId);
	}

	public override ScopePolicy<Project> Policy
		=> ScopePolicy<Project>.Any(
			ScopePolicy<Project>.Grant(ScopeDimensions.Owner),
			ScopePolicy<Project>.Grant(ScopeDimensions.Dept));

	public override void Declare(ScopePolicySet<Project> policies)
	{
		policies.For(ProjectPermissions.Edit, ScopePolicy<Project>.Grant(ScopeDimensions.Owner));
	}
}